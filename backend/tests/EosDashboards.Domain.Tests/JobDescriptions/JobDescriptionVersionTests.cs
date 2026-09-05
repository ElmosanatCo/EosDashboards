using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Tests.JobDescriptions;

public sealed class JobDescriptionVersionTests
{
    private static readonly DateTime Now = new(2026, 9, 4, 10, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void New_version_starts_waiting_for_department_approval_and_reports_missing_optional_data()
    {
        var version = CreateVersion(personnelCode: null, taskStartDate: null);

        Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, version.WorkflowStatus);
        Assert.Equal(JobDescriptionQualityStatus.Incomplete, version.QualityStatus);
    }

    [Fact]
    public void Imported_unmatched_catalog_values_are_retained_and_require_data_completion()
    {
        var version = JobDescriptionVersion.Create(
            "پرسنل نمونه",
            1,
            "EMP-1",
            "لیسانس",
            "مهندسی نرم افزار",
            "۳ سال",
            [1],
            [JobDescriptionTask.Create(10, "توسعه نرم افزار", "شرح وظیفه", new DateOnly(2026, 9, 1), null, 1, 40)],
            Now,
            unresolvedSkillNames: ["مهارت واردشده"],
            unresolvedTasks: [new UnresolvedTaskInput("وظیفه واردشده", "شرح وظیفه واردشده", null, null, 2)]);

        Assert.Equal(JobDescriptionQualityStatus.Incomplete, version.QualityStatus);
        Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, version.WorkflowStatus);
        Assert.Equal("مهارت واردشده", Assert.Single(version.UnresolvedSkills).RawName);
        Assert.Equal("وظیفه واردشده", Assert.Single(version.UnresolvedTasks).RawTitle);
    }

    [Fact]
    public void Department_approval_is_rejected_until_incomplete_data_is_resolved()
    {
        var version = CreateVersion(personnelCode: null, taskStartDate: null);

        var error = Assert.Throws<InvalidOperationException>(() => version.ApproveByDepartmentManager(Now));

        Assert.Contains("ناقص", error.Message);
        Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, version.WorkflowStatus);
    }

    [Fact]
    public void Version_becomes_complete_when_profile_and_task_start_data_are_present()
    {
        var version = CreateVersion("EMP-1", new DateOnly(2026, 9, 1));

        Assert.Equal(JobDescriptionQualityStatus.Healthy, version.QualityStatus);
    }

    [Fact]
    public void Catalog_quality_issue_returns_human_resources_review_to_data_completion()
    {
        var version = CreateVersion("EMP-1", new DateOnly(2026, 9, 1));
        version.ApproveByDepartmentManager(Now);

        version.SetCatalogQualityIssues(true, Now.AddHours(1));

        Assert.Equal(JobDescriptionQualityStatus.Incomplete, version.QualityStatus);
        Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, version.WorkflowStatus);
        Assert.Null(version.DepartmentApprovedAt);
        Assert.Null(version.HumanResourcesReviewedAt);
    }

    [Fact]
    public void Task_with_past_end_date_is_not_active_but_remains_in_version()
    {
        var ended = JobDescriptionTask.Create(
            10,
            "نگهداری نرم افزار",
            "شرح قدیمی",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 8, 31),
            1,
            40);
        var version = CreateVersion("EMP-1", new DateOnly(2026, 9, 1), ended);

        Assert.False(ended.IsActiveOn(new DateOnly(2026, 9, 4)));
        Assert.Contains(ended, version.Tasks);
    }

    [Fact]
    public void Approval_workflow_requires_department_approval_before_human_resources_review()
    {
        var version = CreateVersion("EMP-1", new DateOnly(2026, 9, 1));

        Assert.Throws<InvalidOperationException>(() => version.ApproveByHumanResources(Now));

        version.ApproveByDepartmentManager(Now);
        version.ApproveByHumanResources(Now.AddHours(1));

        Assert.Equal(JobDescriptionWorkflowStatus.Approved, version.WorkflowStatus);
    }

    [Fact]
    public void Department_task_catalog_can_mark_a_task_as_a_project_and_map_required_skills()
    {
        var task = TaskCatalogItem.Create(1, "توسعه نرم افزار", isProject: true, Now);
        var skill = SkillCatalogItem.Create(1, "دات نت", Now);

        task.AddRequiredSkill(7);

        Assert.True(task.IsProject);
        Assert.Contains(7, task.RequiredSkillIds);
    }

    private static JobDescriptionVersion CreateVersion(
        string? personnelCode,
        DateOnly? taskStartDate,
        JobDescriptionTask? task = null)
    {
        return JobDescriptionVersion.Create(
            "علی نمونه",
            1,
            personnelCode,
            "لیسانس",
            "مهندسی نرم افزار",
            "۳ سال",
            [1],
            [task ?? JobDescriptionTask.Create(10, "توسعه نرم افزار", "شرح وظیفه", taskStartDate, null, 1, 40)],
            Now);
    }
}
