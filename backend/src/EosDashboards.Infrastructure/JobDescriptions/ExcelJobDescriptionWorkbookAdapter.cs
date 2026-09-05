using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Infrastructure.JobDescriptions;

public sealed class ExcelJobDescriptionWorkbookParser : IJobDescriptionWorkbookParser
{
    public async Task<ImportedJobDescriptionWorkbook> ParseAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only .xlsx workbooks are supported.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("The workbook has no first worksheet.");
        using var sheetStream = sheet.Open();
        var rows = ReadRows(sheetStream, sharedStrings);
        if (rows.Count == 0) throw new InvalidDataException("The workbook is empty.");

        var profile = ReadProfile(rows);
        var (tasks, _) = ReadTasks(rows);
        return new ImportedJobDescriptionWorkbook(
            fileName,
            profile.PersonName,
            profile.DepartmentName,
            profile.PersonnelCode,
            profile.Education,
            profile.FieldOfStudy,
            profile.MinimumExperience,
            profile.SkillNames,
            tasks);
    }

    private static Dictionary<int, string> ReadSharedStrings(ZipArchive archive)
    {
        var result = new Dictionary<int, string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return result;
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var index = 0;
        foreach (var item in document.Descendants(ns + "si"))
        {
            result[index++] = string.Concat(item.Descendants(ns + "t").Select(text => text.Value));
        }
        return result;
    }

    private static List<string[]> ReadRows(Stream stream, IReadOnlyDictionary<int, string> sharedStrings)
    {
        var document = XDocument.Load(stream);
        var ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var result = new List<string[]>();
        foreach (var row in document.Descendants(ns + "row"))
        {
            var cells = new Dictionary<int, string>();
            var fallbackColumn = 0;
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var column = reference is null ? fallbackColumn : ColumnIndex(reference);
                fallbackColumn = column + 1;
                var type = cell.Attribute("t")?.Value;
                var value = type == "inlineStr"
                    ? string.Concat(cell.Descendants(ns + "t").Select(item => item.Value))
                    : cell.Element(ns + "v")?.Value ?? string.Empty;
                if (type == "s" && int.TryParse(value, out var sharedIndex) && sharedStrings.TryGetValue(sharedIndex, out var sharedValue))
                    value = sharedValue;
                cells[column] = value.Trim();
            }
            result.Add(cells.Count == 0 ? [] : Enumerable.Range(0, cells.Keys.Max() + 1).Select(index => cells.GetValueOrDefault(index, string.Empty)).ToArray());
        }
        return result;
    }

    private static Profile ReadProfile(IReadOnlyList<string[]> rows)
    {
        string? personName = null, departmentName = null, personnelCode = null, education = null, field = null, experience = null;
        var skills = new List<string>();
        foreach (var row in rows.Take(30))
        {
            for (var index = 0; index < row.Length; index++)
            {
                var (rawLabel, inlineValue) = SplitInlineField(row[index]);
                var label = Normalize(rawLabel);
                var value = string.IsNullOrWhiteSpace(inlineValue)
                    ? row.Skip(index + 1).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
                    : inlineValue;
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (Matches(label, "نام پرسنل", "نام و نام خانوادگی", "نام کارمند", "نام"))
                {
                    var embeddedCode = ExtractEmbeddedField(value, "کد پرسنلی");
                    personName ??= embeddedCode.Value;
                    personnelCode ??= embeddedCode.Code;
                }
                else if (Matches(label, "بخش", "واحد", "نام واحد", "دپارتمان", "بخش سازمانی")) departmentName ??= value;
                else if (Matches(label, "کد پرسنلی", "کد کارمندی")) personnelCode ??= value;
                else if (Matches(label, "مدرک تحصیلی", "مدرک")) education ??= value;
                else if (Matches(label, "رشته تحصیلی", "رشته")) field ??= value;
                else if (Matches(label, "حداقل سابقه تخصصی", "حداقل میزان سابقه کار", "سابقه تخصصی", "سابقه")) experience ??= value;
                else if (Matches(label, "مهارت ها", "مهارتها", "مهارت", "مهارتها و توانایی های مورد نیاز", "تخصص ها", "تخصص")) skills.AddRange(SplitList(value));
            }
        }
        return new Profile(personName, departmentName, personnelCode, education, field, experience, skills.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static (string Label, string? InlineValue) SplitInlineField(string cell)
    {
        var separatorIndex = cell.IndexOfAny([':', '：']);
        return separatorIndex < 0
            ? (cell, null)
            : (cell[..separatorIndex], cell[(separatorIndex + 1)..].Trim());
    }

    private static (IReadOnlyList<ImportedTask> Tasks, int HeaderIndex) ReadTasks(IReadOnlyList<string[]> rows)
    {
        var headerIndex = rows.Select((row, index) => (row, index)).Where(item =>
            item.row.Any(cell => Normalize(cell).Contains("شرح", StringComparison.Ordinal) && (Normalize(cell).Contains("وظیف", StringComparison.Ordinal) || Normalize(cell).Contains("وظایف", StringComparison.Ordinal) || Normalize(cell).Contains("کار", StringComparison.Ordinal)))).Select(item => (int?)item.index).FirstOrDefault();
        if (headerIndex is null)
            return ([], -1);

        var header = rows[headerIndex.Value];
        var titleColumn = FindColumn(header, "عنوان وظیفه", "عنوان", "وظیفه", "کار");
        var descriptionColumn = FindColumn(header, "شرح وظیفه", "شرح وظایف", "شرح", "توضیحات");
        var startColumn = FindColumn(header, "تاریخ شروع", "شروع", "تاریخ");
        var endColumn = FindColumn(header, "تاریخ پایان", "پایان");
        var sequenceColumn = FindColumn(header, "ردیف", "شماره ردیف", "شماره");
        var tasks = new List<ImportedTask>();
        for (var rowIndex = headerIndex.Value + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var title = ValueAt(row, titleColumn);
            var description = ValueAt(row, descriptionColumn);
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description)) continue;
            if (string.IsNullOrWhiteSpace(title)) continue;
            var extra = row.Select((value, index) => (value, index))
                .Where(item => !string.IsNullOrWhiteSpace(item.value) && item.index != sequenceColumn && item.index != titleColumn && item.index != descriptionColumn && item.index != startColumn && item.index != endColumn)
                .Select(item => $"ستون {item.index + 1}: {item.value}");
            var fullDescription = JobDescriptionWorkbookTextSanitizer.RemoveSyntheticColumnAnnotations(
                string.Join(" | ", new[] { description }.Concat(extra).Where(value => !string.IsNullOrWhiteSpace(value))));
            if (string.IsNullOrWhiteSpace(fullDescription)) continue;
            tasks.Add(new ImportedTask(title, fullDescription, ParseDate(ValueAt(row, startColumn)), ParseDate(ValueAt(row, endColumn)), tasks.Count + 1));
        }
        return (tasks, headerIndex.Value);
    }

    private static int FindColumn(IReadOnlyList<string> header, params string[] labels) =>
        Array.FindIndex(header.ToArray(), cell => labels.Any(label => Normalize(cell).Contains(Normalize(label), StringComparison.Ordinal)));

    private static string ValueAt(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index] : string.Empty;

    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace('/', '-').Replace('٫', '.');
        normalized = PersianDigits(normalized);
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 80000)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        var dateParts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (dateParts.Length == 3 &&
            int.TryParse(dateParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) &&
            int.TryParse(dateParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var month) &&
            int.TryParse(dateParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) &&
            year is >= 1200 and <= 1700)
        {
            try
            {
                return DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0));
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
        if (DateOnly.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) return DateOnly.FromDateTime(dateTime);
        return null;
    }

    private static IEnumerable<string> SplitList(string value) => value.Split(['،', ',', ';', '؛', '\n', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static (string Value, string? Code) ExtractEmbeddedField(string value, string fieldName)
    {
        var marker = value.IndexOf(fieldName, StringComparison.Ordinal);
        if (marker < 0) return (value, null);
        var separator = value.IndexOf(':', marker);
        if (separator < 0) return (value, null);
        var code = value[(separator + 1)..].Trim().Trim('(', ')', '،', ',').Trim();
        var cleanValue = value[..marker].Trim().Trim('(', ')', '،', ',').Trim();
        return (cleanValue, string.IsNullOrWhiteSpace(code) ? null : code);
    }
    private static bool Matches(string value, params string[] labels) => labels.Any(label => value == Normalize(label));
    private static string Normalize(string value) => PersianDigits(value ?? string.Empty).Replace("ي", "ی").Replace("ك", "ک").Replace("‌", string.Empty).Replace(" ", string.Empty).Replace(":", string.Empty).Trim().ToLowerInvariant();
    private static string PersianDigits(string value) => value.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4').Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
    private static int ColumnIndex(string reference)
    {
        var value = 0;
        foreach (var character in reference.TakeWhile(char.IsLetter)) value = value * 26 + char.ToUpperInvariant(character) - 'A' + 1;
        return value - 1;
    }

    private sealed record Profile(string? PersonName, string? DepartmentName, string? PersonnelCode, string? Education, string? FieldOfStudy, string? MinimumExperience, IReadOnlyList<string> SkillNames);
}

internal static class JobDescriptionWorkbookTextSanitizer
{
    private static readonly Regex SyntheticColumnAnnotation = new(
        @"(?:\s*\|\s*ستون\s+\d+\s*:\s*[\d۰-۹]+\s*)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string RemoveSyntheticColumnAnnotations(string value) =>
        SyntheticColumnAnnotation.Replace(value, string.Empty).Trim();
}

public sealed class ExcelJobDescriptionWorkbookGenerator : IJobDescriptionWorkbookGenerator
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Xml = "http://www.w3.org/XML/1998/namespace";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string TemplateResourceName = "EosDashboards.Infrastructure.Resources.Templates.job-description-reference.xlsx";

    public byte[] Generate(
        JobDescriptionVersion version,
        DateOnly asOf,
        string? departmentName = null,
        IReadOnlyCollection<string>? skillNames = null)
    {
        using var template = new MemoryStream(LoadTemplate(), writable: false);
        using var source = new ZipArchive(template, ZipArchiveMode.Read, leaveOpen: true);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in source.Entries)
            {
                if (entry.FullName == "xl/sharedStrings.xml") continue;
                var target = archive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var input = entry.Open();
                using var outputEntry = target.Open();
                if (entry.FullName == "xl/worksheets/sheet1.xml")
                {
                    var sheet = XDocument.Load(input, LoadOptions.PreserveWhitespace);
                    UpdateSheet(sheet, version, asOf, departmentName, skillNames);
                    sheet.Save(outputEntry, SaveOptions.DisableFormatting);
                }
                else if (entry.FullName == "xl/workbook.xml")
                {
                    var workbook = XDocument.Load(input, LoadOptions.PreserveWhitespace);
                    UpdatePrintArea(workbook, version, asOf);
                    workbook.Save(outputEntry, SaveOptions.DisableFormatting);
                }
                else if (entry.FullName == "[Content_Types].xml")
                {
                    var contentTypes = XDocument.Load(input, LoadOptions.PreserveWhitespace);
                    contentTypes.Root?.Elements(ContentTypes + "Override")
                        .Where(item => (string?)item.Attribute("PartName") == "/xl/sharedStrings.xml")
                        .Remove();
                    contentTypes.Save(outputEntry, SaveOptions.DisableFormatting);
                }
                else if (entry.FullName == "xl/_rels/workbook.xml.rels")
                {
                    var relationships = XDocument.Load(input, LoadOptions.PreserveWhitespace);
                    relationships.Root?.Elements(PackageRelationships + "Relationship")
                        .Where(item => (string?)item.Attribute("Target") == "sharedStrings.xml" ||
                                       ((string?)item.Attribute("Type"))?.EndsWith("/sharedStrings", StringComparison.Ordinal) == true)
                        .Remove();
                    relationships.Save(outputEntry, SaveOptions.DisableFormatting);
                }
                else
                {
                    input.CopyTo(outputEntry);
                }
            }
        }
        return output.ToArray();
    }

    private static byte[] LoadTemplate()
    {
        using var stream = typeof(ExcelJobDescriptionWorkbookGenerator).Assembly
            .GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException("The standard job-description workbook template is unavailable.");
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static void UpdateSheet(
        XDocument document,
        JobDescriptionVersion version,
        DateOnly asOf,
        string? departmentName,
        IReadOnlyCollection<string>? skillNames)
    {
        var sheetData = document.Root?.Element(Spreadsheet + "sheetData")
            ?? throw new InvalidDataException("The standard workbook has no sheet data.");
        var rows = sheetData.Elements(Spreadsheet + "row").ToArray();
        var taskTemplate = rows.Single(row => (string?)row.Attribute("r") == "7");
        foreach (var row in rows.Where(row => ParseRowNumber(row) >= 7)) row.Remove();

        SetCell(rows.Single(row => ParseRowNumber(row) == 1), "A", "شرح شغل ");
        SetCell(rows.Single(row => ParseRowNumber(row) == 2), "A", $"نام پرسنل: {version.PersonName}");
        SetCell(rows.Single(row => ParseRowNumber(row) == 2), "C", $"نام واحد  : {departmentName ?? string.Empty}");
        SetCell(rows.Single(row => ParseRowNumber(row) == 2), "E", $"مدرک تحصیلی : {version.Education}");
        SetCell(rows.Single(row => ParseRowNumber(row) == 3), "A", $"حداقل میزان سابقه کار: {version.MinimumExperience}");
        SetCell(rows.Single(row => ParseRowNumber(row) == 3), "C", $"رشته تحصیلی: {version.FieldOfStudy}");
        var renderedSkills = skillNames ?? version.UnresolvedSkills
            .OrderBy(skill => skill.SortOrder)
            .Select(skill => skill.RawName)
            .ToArray();
        SetCell(rows.Single(row => ParseRowNumber(row) == 4), "A", $"مهارتها  و توانایی های مورد نیاز : {string.Join("، ", renderedSkills)}");
        SetCell(rows.Single(row => ParseRowNumber(row) == 6), "A", "ردیف");
        SetCell(rows.Single(row => ParseRowNumber(row) == 6), "B", "عنوان وظایف");
        SetCell(rows.Single(row => ParseRowNumber(row) == 6), "D", "تاریخ ");
        SetCell(rows.Single(row => ParseRowNumber(row) == 6), "E", "شرح وظایف ");

        var tasks = version.Tasks
            .Where(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf)
            .Select(task => (SortOrder: task.SortOrder, task.Title, task.StartDate, task.EndDate, task.Description))
            .Concat(version.UnresolvedTasks
                .Where(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf)
                .Select(task => (SortOrder: task.SortOrder, Title: task.RawTitle, task.StartDate, task.EndDate, task.Description)))
            .OrderBy(task => task.SortOrder)
            .ToArray();
        var merges = document.Root?.Element(Spreadsheet + "mergeCells");
        foreach (var merge in merges?.Elements(Spreadsheet + "mergeCell").ToArray() ?? [])
        {
            var reference = (string?)merge.Attribute("ref") ?? string.Empty;
            if (reference.StartsWith("B", StringComparison.Ordinal) && ParseRowNumber(reference) >= 7) merge.Remove();
        }

        foreach (var (task, index) in tasks.Select((task, index) => (task, index)))
        {
            var rowNumber = 7 + index;
            var row = new XElement(taskTemplate);
            row.SetAttributeValue("r", rowNumber);
            var templateCells = taskTemplate.Elements(Spreadsheet + "c").ToDictionary(
                cell => (string?)cell.Attribute("r") is { } reference ? reference[..^1] : string.Empty,
                StringComparer.Ordinal);
            row.RemoveNodes();
            row.Add(
                InlineCell(templateCells["A"], "" + (index + 1), $"A{rowNumber}"),
                InlineCell(templateCells["B"], task.Title, $"B{rowNumber}"),
                InlineCell(templateCells["C"], string.Empty, $"C{rowNumber}"),
                InlineCell(templateCells["D"], ToPersianDate(task.StartDate), $"D{rowNumber}"),
                InlineCell(templateCells["E"], JobDescriptionWorkbookTextSanitizer.RemoveSyntheticColumnAnnotations(task.Description), $"E{rowNumber}"));
            sheetData.Add(row);
            merges?.Add(new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"B{rowNumber}:C{rowNumber}")));
        }
    }

    private static void UpdatePrintArea(XDocument document, JobDescriptionVersion version, DateOnly asOf)
    {
        var lastRow = 6 + version.Tasks.Count(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf) +
            version.UnresolvedTasks.Count(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf);
        var definedName = document.Descendants(Spreadsheet + "definedName")
            .FirstOrDefault(item => (string?)item.Attribute("name") == "_xlnm.Print_Area");
        if (definedName is not null) definedName.Value = $"Sheet1!$A$1:$E${Math.Max(lastRow, 6)}";
    }

    private static int ParseRowNumber(XElement rowOrReference)
    {
        var value = (string?)rowOrReference.Attribute("r") ?? string.Empty;
        return ParseRowNumber(value);
    }

    private static int ParseRowNumber(string reference)
    {
        var digits = new string(reference.SkipWhile(character => !char.IsDigit(character)).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }

    private static void SetCell(XElement row, string column, string value)
    {
        var cell = row.Elements(Spreadsheet + "c")
            .FirstOrDefault(item => ((string?)item.Attribute("r"))?.StartsWith(column, StringComparison.Ordinal) == true);
        if (cell is not null) ReplaceCellValue(cell, value);
    }

    private static XElement InlineCell(XElement template, string value, string reference)
    {
        var cell = new XElement(template);
        cell.SetAttributeValue("r", reference);
        ReplaceCellValue(cell, value);
        return cell;
    }

    private static void ReplaceCellValue(XElement cell, string value)
    {
        cell.SetAttributeValue("t", "inlineStr");
        cell.Element(Spreadsheet + "v")?.Remove();
        cell.Element(Spreadsheet + "is")?.Remove();
        cell.Add(new XElement(
            Spreadsheet + "is",
            new XElement(Spreadsheet + "t", new XAttribute(Xml + "space", "preserve"), value)));
    }

    private static string ToPersianDate(DateOnly? value)
    {
        if (value is null) return string.Empty;
        var date = value.Value.ToDateTime(TimeOnly.MinValue);
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}";
    }
}
