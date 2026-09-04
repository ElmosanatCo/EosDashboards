using System.IO.Compression;
using System.Security;
using System.Text;
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
        Assert.Equal("P-7", parsed.PersonnelCode);
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
            ["۱", "نگهداری و توسعه Wintas", "", "۱۴۰۲/۰۲/۱۱", "بررسی باگ های ثبت شده"]
        ]);

        var parsed = await parser.ParseAsync("پرهام جهانشاهی.xlsx", workbook, CancellationToken.None);

        Assert.Equal("پرهام جهانشاهی", parsed.PersonName);
        Assert.Equal("نرم افزار", parsed.DepartmentName);
        Assert.Equal("لیسانس", parsed.Education);
        Assert.Equal("2 سال", parsed.MinimumExperience);
        Assert.Contains("delphi", parsed.SkillNames);
        var task = Assert.Single(parsed.Tasks);
        Assert.Equal("نگهداری و توسعه Wintas", task.Title);
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
        Assert.Contains(parsed.Tasks, task => task.Title == "وظیفه تایپی" && task.Description.Contains("شرح تایپی"));
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
