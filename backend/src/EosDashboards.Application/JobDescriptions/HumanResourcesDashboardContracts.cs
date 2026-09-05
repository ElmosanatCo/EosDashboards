using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.JobDescriptions;

public sealed record HumanResourcesMetricSet(
    int PersonnelCount,
    int ActivePersonnelCount,
    int ArchivedPersonnelCount,
    int HealthyDescriptionCount,
    int IncompleteDescriptionCount,
    int PendingDataCompletionCount,
    int PendingDepartmentApprovalCount,
    int UnderHumanResourcesReviewCount,
    int ApprovedDescriptionCount,
    int RejectedDescriptionCount,
    int ActiveProjectCount,
    int PeopleWorkingOnActiveProjectsCount);

public sealed record HumanResourcesChangeSummary(
    long DepartmentId,
    string DepartmentName,
    int ChangeCount,
    DateTime? LatestChangedAt);

public sealed record HumanResourcesChangeItem(
    long VersionId,
    long DepartmentId,
    string DepartmentName,
    string PersonName,
    string ChangeType,
    DateTime ChangedAt,
    long? ActorUserId);

public sealed record HumanResourcesDashboardResult(
    HumanResourcesMetricSet Metrics,
    IReadOnlyList<HumanResourcesChangeSummary> ChangeSummaries,
    IReadOnlyList<HumanResourcesChangeItem> Changes,
    int TotalChangeCount,
    int Page,
    int PageSize);

public sealed record JobDescriptionComparisonVersions(
    JobDescriptionVersion Current,
    JobDescriptionVersion? Previous);

public sealed record JobDescriptionComparisonSnapshot(
    long VersionId,
    string PersonName,
    long DepartmentId,
    string? PersonnelCode,
    string Education,
    string FieldOfStudy,
    string MinimumExperience,
    IReadOnlyList<long> SkillIds,
    IReadOnlyList<JobDescriptionComparisonTaskSnapshot> Tasks,
    JobDescriptionWorkflowStatus WorkflowStatus,
    JobDescriptionQualityStatus QualityStatus,
    DateTime UpdatedAt);

public sealed record JobDescriptionComparisonTaskSnapshot(
    long TaskCatalogItemId,
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder,
    decimal? WeeklyHours);

public sealed record JobDescriptionComparisonChange(
    string Field,
    string Kind,
    string? Before,
    string? After);

public sealed record JobDescriptionComparisonResult(
    long CurrentVersionId,
    long? PreviousVersionId,
    JobDescriptionComparisonSnapshot Current,
    JobDescriptionComparisonSnapshot? Previous,
    IReadOnlyList<JobDescriptionComparisonChange> Changes);

public interface IHumanResourcesDashboardReader
{
    Task<IReadOnlyList<ManagedDepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken);

    Task<HumanResourcesDashboardResult> GetAsync(
        long? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IJobDescriptionComparisonReader
{
    Task<JobDescriptionComparisonVersions?> GetAsync(
        long versionId,
        CancellationToken cancellationToken);
}
