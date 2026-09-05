using EosDashboards.Domain.Enums;

namespace EosDashboards.Api.JobDescriptions;

public sealed record CreateJobDescriptionRequest(
    string PersonName,
    long DepartmentId,
    string PersonnelCode,
    string Education,
    string FieldOfStudy,
    string MinimumExperience,
    long[] SkillIds,
    JobDescriptionTaskRequest[] Tasks);

public sealed record JobDescriptionTaskRequest(
    long TaskCatalogItemId,
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder,
    decimal? WeeklyHours = null);

public sealed record RejectJobDescriptionRequest(string Reason);

public sealed record JobDescriptionListResponse(
    long Id,
    long DepartmentId,
    string PersonName,
    string WorkflowStatus,
    string QualityStatus,
    DateTime UpdatedAt);

public sealed record JobDescriptionOperationResponse(
    long Id,
    string WorkflowStatus,
    string QualityStatus,
    string? RejectionReason);

public sealed record JobDescriptionDetailResponse(
    long Id,
    long DepartmentId,
    string PersonName,
    string? PersonnelCode,
    string Education,
    string FieldOfStudy,
    string MinimumExperience,
    IReadOnlyCollection<long> SkillIds,
    IReadOnlyCollection<JobDescriptionTaskResponse> Tasks,
    IReadOnlyCollection<JobDescriptionUnresolvedSkillResponse> UnresolvedSkills,
    IReadOnlyCollection<JobDescriptionUnresolvedTaskResponse> UnresolvedTasks,
    string WorkflowStatus,
    string QualityStatus,
    string? RejectionReason);

public sealed record JobDescriptionTaskResponse(
    long TaskCatalogItemId,
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder,
    decimal? WeeklyHours);

public sealed record JobDescriptionUnresolvedSkillResponse(string RawName, int SortOrder);

public sealed record JobDescriptionUnresolvedTaskResponse(
    string RawTitle,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder);

public sealed record JobDescriptionCatalogResponse(
    IReadOnlyList<SkillCatalogResponse> Skills,
    IReadOnlyList<TaskCatalogResponse> Tasks);

public sealed record SkillCatalogResponse(
    long Id,
    long? DepartmentId,
    string Name,
    long? OwnerDepartmentId,
    int UsageDepartmentCount,
    bool IsActive,
    bool CanEdit,
    bool CanDelete);

public sealed record TaskCatalogResponse(long Id, long DepartmentId, string Title, bool IsProject, bool IsActive, IReadOnlyCollection<long> RequiredSkillIds);

public sealed record CreateSkillRequest(long DepartmentId, string Name);

public sealed record CreatePublicSkillRequest(long OwnerDepartmentId, string Name);

public sealed record CreateTaskRequest(long DepartmentId, string Title, bool IsProject);

public sealed record SetTaskRequiredSkillsRequest(long[] SkillIds);

public sealed record UpdatePublicSkillRequest(string Name);

public sealed record UpdateCatalogNameRequest(string Name);

public sealed record MergePublicSkillRequest(long SurvivingSkillId);

public sealed record HumanResourcesDashboardResponse(
    HumanResourcesMetricResponse Metrics,
    IReadOnlyList<HumanResourcesChangeSummaryResponse> ChangeSummaries,
    IReadOnlyList<HumanResourcesChangeResponse> Changes,
    int TotalChangeCount,
    int Page,
    int PageSize);

public sealed record HumanResourcesMetricResponse(
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

public sealed record HumanResourcesChangeSummaryResponse(
    long DepartmentId,
    string DepartmentName,
    int ChangeCount,
    DateTime? LatestChangedAt);

public sealed record HumanResourcesChangeResponse(
    long VersionId,
    long DepartmentId,
    string DepartmentName,
    string PersonName,
    string ChangeType,
    DateTime ChangedAt,
    long? ActorUserId);

public sealed record JobDescriptionComparisonResponse(
    long CurrentVersionId,
    long? PreviousVersionId,
    JobDescriptionComparisonSnapshotResponse Current,
    JobDescriptionComparisonSnapshotResponse? Previous,
    IReadOnlyList<JobDescriptionComparisonChangeResponse> Changes);

public sealed record JobDescriptionComparisonSnapshotResponse(
    long VersionId,
    string PersonName,
    long DepartmentId,
    string? PersonnelCode,
    string Education,
    string FieldOfStudy,
    string MinimumExperience,
    IReadOnlyList<long> SkillIds,
    IReadOnlyList<JobDescriptionComparisonTaskSnapshotResponse> Tasks,
    string WorkflowStatus,
    string QualityStatus,
    DateTime UpdatedAt);

public sealed record JobDescriptionComparisonTaskSnapshotResponse(
    long TaskCatalogItemId,
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder,
    decimal? WeeklyHours);

public sealed record JobDescriptionComparisonChangeResponse(
    string Field,
    string Kind,
    string? Before,
    string? After);
