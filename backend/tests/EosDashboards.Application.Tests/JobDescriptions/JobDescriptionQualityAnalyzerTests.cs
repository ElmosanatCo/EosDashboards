using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class JobDescriptionQualityAnalyzerTests
{
    private static readonly DateTime Now = new(2026, 9, 4, 10, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Reports_missing_required_skill_with_a_task_skill_action_target()
    {
        var taskCatalog = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: false, Now);
        SetId(taskCatalog, 1);
        taskCatalog.AddRequiredSkill(20);
        var version = CreateVersion(skillIds: [10]);

        var findings = JobDescriptionQualityAnalyzer.Analyze(version, [taskCatalog]);

        var finding = Assert.Single(findings, item => item.Code == "missing-required-skill");
        Assert.Equal("task:1/skills", finding.ActionTarget);
        Assert.Contains("20", finding.Message);
    }

    [Fact]
    public void Reports_selected_skill_without_supporting_task()
    {
        var taskCatalog = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: false, Now);
        SetId(taskCatalog, 1);
        var version = CreateVersion(skillIds: [10]);

        var findings = JobDescriptionQualityAnalyzer.Analyze(version, [taskCatalog]);

        var finding = Assert.Single(findings, item => item.Code == "unsupported-selected-skill");
        Assert.Equal("skills", finding.ActionTarget);
    }

    [Fact]
    public void Uses_skill_names_in_catalog_quality_messages_when_they_are_available()
    {
        var taskCatalog = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: false, Now);
        SetId(taskCatalog, 1);
        taskCatalog.AddRequiredSkill(20);
        var version = CreateVersion(skillIds: [10]);

        var findings = JobDescriptionQualityAnalyzer.Analyze(
            version,
            [taskCatalog],
            new Dictionary<long, string> { [10] = "آموزش", [20] = "مدیریت پروژه" });

        Assert.Contains(findings, item => item.Code == "missing-required-skill" && item.Message.Contains("مدیریت پروژه"));
        Assert.Contains(findings, item => item.Code == "unsupported-selected-skill" && item.Message.Contains("آموزش"));
    }

    [Fact]
    public void Reports_missing_task_start_date_at_the_task_location()
    {
        var taskCatalog = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: false, Now);
        SetId(taskCatalog, 1);
        var version = CreateVersion(skillIds: [10], startDate: null);

        var findings = JobDescriptionQualityAnalyzer.Analyze(version, [taskCatalog]);

        var finding = Assert.Single(findings, item => item.Code == "missing-task-start-date");
        Assert.Equal("task:1/startDate", finding.ActionTarget);
    }

    [Fact]
    public void Reports_missing_weekly_workload_at_the_task_location()
    {
        var taskCatalog = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: false, Now);
        SetId(taskCatalog, 1);
        var version = JobDescriptionVersion.Create(
            "علی نمونه", 1, "EMP-1", "لیسانس", "مهندسی نرم افزار", "۳ سال", [10],
            [JobDescriptionTask.Create(1, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1)],
            Now);

        var findings = JobDescriptionQualityAnalyzer.Analyze(version, [taskCatalog]);

        var finding = Assert.Single(findings, item => item.Code == "missing-weekly-hours");
        Assert.Equal("task:1/weeklyHours", finding.ActionTarget);
    }

    [Fact]
    public void Reports_unresolved_imported_skill_and_task()
    {
        var version = JobDescriptionVersion.Create(
            "علی نمونه", 1, "EMP-1", "لیسانس", "مهندسی نرم افزار", "۳ سال", [10],
            [JobDescriptionTask.Create(1, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1, 40)],
            Now,
            unresolvedSkillNames: ["مهارت تایپی"],
            unresolvedTasks: [new UnresolvedTaskInput("وظیفه تایپی", "شرح تایپی", new DateOnly(2026, 9, 1), null, 2)]);

        var findings = JobDescriptionQualityAnalyzer.Analyze(version, []);

        Assert.Contains(findings, item => item.Code == "unresolved-skill");
        Assert.Contains(findings, item => item.Code == "unresolved-task");
    }

    private static JobDescriptionVersion CreateVersion(IReadOnlyCollection<long> skillIds, DateOnly? startDate = null) =>
        JobDescriptionVersion.Create(
            "علی نمونه",
            1,
            "EMP-1",
            "لیسانس",
            "مهندسی نرم افزار",
            "۳ سال",
            skillIds,
            [JobDescriptionTask.Create(1, "توسعه نرم افزار", "شرح", startDate, null, 1, 40)],
            Now);

    private static void SetId<T>(T entity, long id) =>
        typeof(T).GetProperty(nameof(TaskCatalogItem.Id))!.SetValue(entity, id);
}
