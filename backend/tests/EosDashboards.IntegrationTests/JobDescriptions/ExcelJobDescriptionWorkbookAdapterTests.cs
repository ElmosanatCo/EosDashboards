using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml.Linq;
using EosDashboards.Domain.Entities;
using EosDashboards.Infrastructure.JobDescriptions;

namespace EosDashboards.IntegrationTests.JobDescriptions;

public sealed class ExcelJobDescriptionWorkbookAdapterTests
{
    [Fact]
    public async Task Generated_standard_workbook_can_be_read_back_without_losing_task_content()
    {
        var version = JobDescriptionVersion.Create(
            "پرسنل نمونه",
            7,
            "P-7",
            "کارشناسی",
            "نرم افزار",
            "سه سال",
            [11, 12],
            [JobDescriptionTask.Create(21, "پشتیبانی سامانه", "پاسخ‌گویی به درخواست‌ها", new DateOnly(2026, 9, 1), null, 1, 40)],
            new DateTime(2026, 9, 4));

        var generator = new ExcelJobDescriptionWorkbookGenerator();
        var parser = new ExcelJobDescriptionWorkbookParser();
        await using var workbook = new MemoryStream(generator.Generate(version, new DateOnly(2026, 9, 4)));
        var parsed = await parser.ParseAsync("نمونه.xlsx", workbook, CancellationToken.None);

        Assert.Equal("پرسنل نمونه", parsed.PersonName);
        Assert.Null(parsed.PersonnelCode);
        var task = Assert.Single(parsed.Tasks);
        Assert.Equal("پشتیبانی سامانه", task.Title);
        Assert.Contains("درخواست‌ها", task.Description);
        Assert.Equal(new DateOnly(2026, 9, 1), task.StartDate);
    }

    [Fact]
    public async Task Inline_profile_labels_are_read_from_realistic_workbook_rows()
    {
        var parser = new ExcelJobDescriptionWorkbookParser();
        await using var workbook = CreateWorkbook(
        [
            ["نام پرسنل: پرهام جهانشاهی", "", "نام واحد  : نرم افزار", "", "مدرک تحصیلی : لیسانس"],
            ["حداقل میزان سابقه کار: 2 سال", "", "رشته تحصیلی: مهندسی کامپیوتر - نرم افزار"],
            ["مهارتها  و توانایی های مورد نیاز :مسلط به دات نت فریموورک، delphi، Sql و Rest Api"],
            ["ردیف", "عنوان وظایف", "", "تاریخ ", "شرح وظایف "],
            ["۱", "نگهداری و توسعه Wintas", "", "۱۴۰۲/۰۲/۱۱", "بررسی باگ های ثبت شده | ستون ۱: ۱"]
        ]);

        var parsed = await parser.ParseAsync("پرهام جهانشاهی.xlsx", workbook, CancellationToken.None);

        Assert.Equal("پرهام جهانشاهی", parsed.PersonName);
        Assert.Equal("نرم افزار", parsed.DepartmentName);
        Assert.Equal("لیسانس", parsed.Education);
        Assert.Equal("2 سال", parsed.MinimumExperience);
        Assert.Contains("delphi", parsed.SkillNames);
        var task = Assert.Single(parsed.Tasks);
        Assert.Equal("نگهداری و توسعه Wintas", task.Title);
        Assert.DoesNotContain("ستون", task.Description);
        Assert.Equal("بررسی باگ های ثبت شده", task.Description);
        Assert.Equal(new DateOnly(2023, 5, 1), task.StartDate);
    }

    [Fact]
    public async Task Generated_workbook_retains_unresolved_skill_and_task_text()
    {
        var version = JobDescriptionVersion.Create(
            "پرسنل نمونه", 7, "P-7", "کارشناسی", "نرم افزار", "سه سال", [11],
            [JobDescriptionTask.Create(21, "پشتیبانی سامانه", "شرح", new DateOnly(2026, 9, 1), null, 1, 40)],
            new DateTime(2026, 9, 4),
            unresolvedSkillNames: ["مهارت تایپی"],
            unresolvedTasks: [new UnresolvedTaskInput("وظیفه تایپی", "شرح تایپی", new DateOnly(2026, 9, 1), null, 2)]);

        var generator = new ExcelJobDescriptionWorkbookGenerator();
        var parser = new ExcelJobDescriptionWorkbookParser();
        await using var workbook = new MemoryStream(generator.Generate(version, new DateOnly(2026, 9, 4)));
        var parsed = await parser.ParseAsync("ناقص.xlsx", workbook, CancellationToken.None);

        Assert.Contains("مهارت تایپی", parsed.SkillNames);
        Assert.Single(parsed.SkillNames, skill => skill == "مهارت تایپی");
        Assert.Contains(parsed.Tasks, task => task.Title == "وظیفه تایپی" && task.Description.Contains("شرح تایپی"));
    }

    [Fact]
    public void Generated_workbook_preserves_the_reference_layout_and_styles()
    {
        var version = JobDescriptionVersion.Create(
            "پرهام جهانشاهی", 7, "P-7", "لیسانس", "نرم افزار", "2 سال", [11],
            [JobDescriptionTask.Create(21, "نگهداری و توسعه Wintas", "شرح وظیفه | ستون 1: 7", new DateOnly(2023, 5, 1), null, 1, 40)],
            new DateTime(2026, 9, 4));

        var workbook = new ExcelJobDescriptionWorkbookGenerator().Generate(
            version,
            new DateOnly(2026, 9, 4),
            "نرم افزار",
            ["دات نت", "SQL"]);

        using var archive = new ZipArchive(new MemoryStream(workbook), ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("xl/styles.xml"));
        Assert.NotNull(archive.GetEntry("xl/theme/theme1.xml"));
        Assert.Null(archive.GetEntry("xl/sharedStrings.xml"));
        Assert.DoesNotContain("sharedStrings", ReadEntry(archive, "xl/_rels/workbook.xml.rels"));
        Assert.DoesNotContain("/xl/sharedStrings.xml", ReadEntry(archive, "[Content_Types].xml"));
        var sheet = ReadEntry(archive, "xl/worksheets/sheet1.xml");
        var workbookXml = ReadEntry(archive, "xl/workbook.xml");
        Assert.Contains("name=\"Sheet1\"", workbookXml);
        Assert.Contains("rightToLeft=\"1\"", sheet);
        Assert.Contains("<cols><col min=\"1\" max=\"1\" width=\"5.5703125\"", sheet);
        Assert.Contains("<mergeCells", sheet);
        Assert.Contains("نام پرسنل: پرهام جهانشاهی", sheet);
        Assert.DoesNotContain("کد پرسنلی", sheet);
        Assert.DoesNotContain("ستون 1: 7", sheet);
        Assert.Contains("عنوان وظایف", sheet);
        Assert.Contains("1402/02/11", sheet);
        var mergeReferences = XDocument.Parse(sheet)
            .Descendants(XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main") + "mergeCell")
            .Select(element => (string?)element.Attribute("ref"))
            .ToArray();
        Assert.Equal(mergeReferences.Distinct(StringComparer.Ordinal).Count(), mergeReferences.Length);
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd();
    }

    private static MemoryStream CreateWorkbook(IReadOnlyList<string[]> rows)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                writer.Write($"<row r=\"{rowIndex + 1}\">");
                foreach (var value in rows[rowIndex])
                {
                    writer.Write($"<c t=\"inlineStr\"><is><t xml:space=\"preserve\">{SecurityElement.Escape(value) ?? string.Empty}</t></is></c>");
                }
                writer.Write("</row>");
            }
            writer.Write("</sheetData></worksheet>");
        }
        stream.Position = 0;
        return stream;
    }
}
