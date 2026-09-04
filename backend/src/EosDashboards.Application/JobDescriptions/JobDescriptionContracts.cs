using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.JobDescriptions;

public sealed record JobDescriptionTaskInput(
    long TaskCatalogItemId,
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder,
    decimal? WeeklyHours = null);

public sealed record CreateJobDescriptionCommand(
    string PersonName,
    long DepartmentId,
    string PersonnelCode,
    string Education,
    string FieldOfStudy,
    string MinimumExperience,
    IReadOnlyCollection<long> SkillIds,
    IReadOnlyCollection<JobDescriptionTaskInput> Tasks);

public sealed record JobDescriptionListItem(
    long Id,
    long DepartmentId,
    string PersonName,
    JobDescriptionWorkflowStatus WorkflowStatus,
    JobDescriptionQualityStatus QualityStatus,
    DateTime UpdatedAt);

public enum JobDescriptionOperationStatus
{
    Succeeded,
    Invalid,
    NotFound,
    Forbidden,
    Conflict,
    Incomplete,
}

public sealed record JobDescriptionOperationResult(
    JobDescriptionOperationStatus Status,
    JobDescriptionVersion? Version = null);

public interface IJobDescriptionRepository
{
    Task<JobDescriptionVersion?> GetForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobDescriptionListItem>> ListAsync(
        IReadOnlyCollection<long> departmentIds,
        long? departmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobDescriptionListItem>> ListForHumanResourcesAsync(CancellationToken cancellationToken);

    void AddRecord(JobDescriptionRecord record);

    void AddVersion(JobDescriptionVersion version);
}

public interface IJobDescriptionCatalogReader
{
    Task<bool> AreValidSelectionsAsync(
        long departmentId,
        IReadOnlyCollection<long> skillIds,
        IReadOnlyCollection<long> taskCatalogItemIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(
        IReadOnlyCollection<long> departmentIds,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(
        IReadOnlyCollection<long> departmentIds,
        long? departmentId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<SkillCatalogItem?> FindSkillByNameAsync(
        long? departmentId,
        string name,
        long? excludingId,
        CancellationToken cancellationToken);

    Task<TaskCatalogItem?> FindTaskByTitleAsync(
        long departmentId,
        string title,
        long? excludingId,
        CancellationToken cancellationToken);

    Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken);
    Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<long>> GetSkillUsageDepartmentIdsAsync(long skillId, CancellationToken cancellationToken);

    Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken);

    void AddSkill(SkillCatalogItem skill);

    void AddTask(TaskCatalogItem task);
}

public interface IHumanResourcesCatalogReader
{
    Task<IReadOnlyList<SkillCatalogListItem>> ListPublicSkillsAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<SkillCatalogItem?> GetPublicSkillForUpdateAsync(long id, CancellationToken cancellationToken);
}

public sealed record SkillCatalogListItem(
    long Id,
    long? DepartmentId,
    string Name,
    long? OwnerDepartmentId,
    bool IsActive,
    IReadOnlyCollection<long> UsageDepartmentIds);

public sealed record TaskCatalogListItem(long Id, long DepartmentId, string Title, bool IsProject, bool IsActive, IReadOnlyCollection<long> RequiredSkillIds);

public sealed record ManagedDepartmentListItem(long Id, string Name, bool IsOwnDepartment);

public sealed record CreateSkillCommand(long DepartmentId, string Name);

public sealed record CreatePublicSkillCommand(long OwnerDepartmentId, string Name);

public sealed record CreateTaskCommand(long DepartmentId, string Title, bool IsProject);

public sealed record SetTaskRequiredSkillsCommand(long TaskId, IReadOnlyCollection<long> SkillIds);

public enum CatalogOperationStatus { Succeeded, Invalid, NotFound, Forbidden, Conflict, Duplicate, InactiveDuplicate }

public sealed record CatalogOperationResult(CatalogOperationStatus Status, long? Id = null);

public interface IJobDescriptionScope
{
    Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken);

    Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken);

    Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken);
}

public interface IJobDescriptionDepartmentReader
{
    Task<IReadOnlyList<ManagedDepartmentListItem>> ListAsync(
        long ownDepartmentId,
        IReadOnlyCollection<long> departmentIds,
        CancellationToken cancellationToken);
}

public interface IJobDescriptionAnalysisReader
{
    Task<IReadOnlyList<TaskCatalogItem>> GetTasksAsync(
        long departmentId,
        IReadOnlyCollection<long> taskIds,
        CancellationToken cancellationToken);
}
