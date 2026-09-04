using System.Globalization;
using System.IO.Compression;
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
                if (Matches(label, "نام پرسنل", "نام و نام خانوادگی", "نام کارمند", "نام")) personName ??= value;
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
        var tasks = new List<ImportedTask>();
        for (var rowIndex = headerIndex.Value + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var title = ValueAt(row, titleColumn);
            var description = ValueAt(row, descriptionColumn);
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description)) continue;
            if (string.IsNullOrWhiteSpace(title)) continue;
            var extra = row.Select((value, index) => (value, index))
                .Where(item => !string.IsNullOrWhiteSpace(item.value) && item.index != titleColumn && item.index != descriptionColumn && item.index != startColumn && item.index != endColumn)
                .Select(item => $"ستون {item.index + 1}: {item.value}");
            var fullDescription = string.Join(" | ", new[] { description }.Concat(extra).Where(value => !string.IsNullOrWhiteSpace(value)));
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

public sealed class ExcelJobDescriptionWorkbookGenerator : IJobDescriptionWorkbookGenerator
{
    public byte[] Generate(JobDescriptionVersion version, DateOnly asOf)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Add(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"شرح وظایف\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var rows = new List<string[]>
            {
                new[] { "نام و نام خانوادگی", version.PersonName },
                new[] { "بخش", version.DepartmentId.ToString(CultureInfo.InvariantCulture) },
                new[] { "کد پرسنلی", version.PersonnelCode ?? "" },
                new[] { "مدرک تحصیلی", version.Education },
                new[] { "رشته تحصیلی", version.FieldOfStudy },
                new[] { "حداقل سابقه تخصصی", version.MinimumExperience },
                new[] { "مهارت‌ها", string.Join("، ", version.SkillIds.Select(skillId => skillId.ToString(CultureInfo.InvariantCulture)).Concat(version.UnresolvedSkills.OrderBy(skill => skill.SortOrder).Select(skill => skill.RawName))) },
                Array.Empty<string>(),
                new[] { "ردیف", "عنوان وظیفه", "تاریخ شروع", "تاریخ پایان", "شرح وظیفه" }
            };
            rows.AddRange(version.Tasks
                .Where(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf)
                .Select(task => (SortOrder: task.SortOrder, Title: task.Title, task.StartDate, task.EndDate, Description: task.Description))
                .Concat(version.UnresolvedTasks
                    .Where(task => !task.EndDate.HasValue || task.EndDate.Value >= asOf)
                    .Select(task => (SortOrder: task.SortOrder, Title: task.RawTitle, task.StartDate, task.EndDate, Description: task.Description)))
                .OrderBy(task => task.SortOrder)
                .Select(task => new[]
                {
                    task.SortOrder.ToString(CultureInfo.InvariantCulture), task.Title, task.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", task.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", task.Description
                }));
            var renderedRows = string.Concat(rows.Select((row, index) =>
                $"<row r=\"{index + 1}\">{string.Concat(row.Select((value, column) => Cell(column, value)))}</row>"));
            var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" + renderedRows + "</sheetData></worksheet>";
            Add(archive, "xl/worksheets/sheet1.xml", sheet);
        }
        return output.ToArray();
    }

    private static string Cell(int column, string value) => $"<c r=\"{ColumnName(column)}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{System.Security.SecurityElement.Escape(value) ?? string.Empty}</t></is></c>";
    private static string ColumnName(int column) { var result = string.Empty; for (var value = column + 1; value > 0; value = (value - 1) / 26) result = (char)('A' + (value - 1) % 26) + result; return result; }
    private static void Add(ZipArchive archive, string name, string content) { using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new System.Text.UTF8Encoding(false)); writer.Write(content); }
}
