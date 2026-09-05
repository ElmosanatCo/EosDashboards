using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionRepository(EosDashboardDbContext context) : IJobDescriptionRepository, IJobDescriptionCatalogReader, IHumanResourcesCatalogReader, IJobDescriptionComparisonReader, IJobDescriptionReviewWarningReader
{
    public Task<JobDescriptionVersion?> GetForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.JobDescriptionVersions
            .Include(item => item.JobDescriptionRecord)
            .Include(item => item.Tasks)
            .Include(item => item.Skills)
            .Include(item => item.UnresolvedSkills)
            .Include(item => item.UnresolvedTasks)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task DeleteVersionAsync(JobDescriptionVersion version, CancellationToken cancellationToken)
    {
        context.JobDescriptionTasks.RemoveRange(version.Tasks);
        context.JobDescriptionVersionSkills.RemoveRange(version.Skills);
        context.JobDescriptionVersionUnresolvedSkills.RemoveRange(version.UnresolvedSkills);
        context.JobDescriptionVersionUnresolvedTasks.RemoveRange(version.UnresolvedTasks);

        var recordId = version.JobDescriptionRecordId;
        context.JobDescriptionVersions.Remove(version);
        if (recordId is not null && !await context.JobDescriptionVersions.AnyAsync(
                item => item.JobDescriptionRecordId == recordId && item.Id != version.Id,
                cancellationToken))
        {
            if (version.JobDescriptionRecord is not null)
            {
                context.JobDescriptionRecords.Remove(version.JobDescriptionRecord);
            }
            else
            {
                var record = await context.JobDescriptionRecords.FindAsync([recordId.Value], cancellationToken);
                if (record is not null) context.JobDescriptionRecords.Remove(record);
            }
        }
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>> ListAsync(
        IReadOnlyCollection<long> departmentIds,
        long? departmentId,
        CancellationToken cancellationToken)
    {
        var query = context.JobDescriptionVersions.AsNoTracking()
            .Include(item => item.Tasks)
            .Include(item => item.Skills)
            .Include(item => item.UnresolvedSkills)
            .Include(item => item.UnresolvedTasks)
            .Where(item => departmentIds.Contains(item.DepartmentId));
        if (departmentId is { } selectedDepartmentId)
        {
            query = query.Where(item => item.DepartmentId == selectedDepartmentId);
        }

        var versions = await query
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var currentVersions = versions
            .GroupBy(item => item.JobDescriptionRecordId is { } recordId
                ? $"record:{recordId}"
                : $"version:{item.Id}")
            .Select(group => group.First());

        return currentVersions.Select(item => new JobDescriptionListItem(
            item.Id,
            item.DepartmentId,
            item.PersonName,
            item.WorkflowStatus,
            item.QualityStatus,
            item.UpdatedAt,
            item.NeedsReview)).ToArray();
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>> ListForHumanResourcesAsync(long? departmentId, CancellationToken cancellationToken)
    {
        var query = context.JobDescriptionVersions.AsNoTracking()
            .Where(item => item.WorkflowStatus == EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.UnderHumanResourcesReview);
        if (departmentId is not null)
        {
            query = query.Where(item => item.DepartmentId == departmentId.Value);
        }

        var versions = await query
            .OrderBy(item => item.UpdatedAt)
            .ToListAsync(cancellationToken);
        return versions.Select(item => new JobDescriptionListItem(
            item.Id, item.DepartmentId, item.PersonName, item.WorkflowStatus, item.QualityStatus, item.UpdatedAt, item.NeedsReview)).ToArray();
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>> ListApprovedForHumanResourcesAsync(long? departmentId, CancellationToken cancellationToken)
    {
        var query = context.JobDescriptionVersions.AsNoTracking()
            .Where(item => item.WorkflowStatus == EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.Approved);
        if (departmentId is not null)
        {
            query = query.Where(item => item.DepartmentId == departmentId.Value);
        }

        var versions = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        return versions
            .GroupBy(item => item.JobDescriptionRecordId ?? item.Id)
            .Select(group => group.First())
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new JobDescriptionListItem(
                item.Id, item.DepartmentId, item.PersonName, item.WorkflowStatus, item.QualityStatus, item.UpdatedAt, item.NeedsReview))
            .ToArray();
    }

    public async Task RevalidateActiveJobDescriptionsAsync(long changedTaskId, DateTime occurredAt, CancellationToken cancellationToken)
    {
        var versions = await WithDetails(context.JobDescriptionVersions)
            .Where(item => item.WorkflowStatus != EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.Approved &&
                           item.WorkflowStatus != EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.Archived &&
                           item.Tasks.Any(task => task.TaskCatalogItemId == changedTaskId))
            .ToArrayAsync(cancellationToken);

        var taskIds = versions
            .SelectMany(version => version.Tasks)
            .Select(task => task.TaskCatalogItemId)
            .Distinct()
            .ToArray();
        var taskCatalog = await context.TaskCatalogItems
            .Include(task => task.RequiredSkills)
            .Where(task => taskIds.Contains(task.Id))
            .ToArrayAsync(cancellationToken);
        var catalogByDepartment = taskCatalog
            .GroupBy(task => task.DepartmentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<TaskCatalogItem>)group.ToArray());

        foreach (var version in versions)
        {
            var departmentCatalog = catalogByDepartment.TryGetValue(version.DepartmentId, out var items)
                ? items
                : [];
            var assessment = JobDescriptionQualityAssessment.From(
                JobDescriptionQualityAnalyzer.Analyze(version, departmentCatalog));
            version.SetCatalogQualityAssessment(
                assessment.HasBlockingIssues,
                assessment.NeedsReview,
                occurredAt);
        }
    }

    public async Task<IReadOnlyList<JobDescriptionReviewWarning>> ListAsync(
        IReadOnlyCollection<long>? departmentIds,
        CancellationToken cancellationToken)
    {
        var versionQuery = context.JobDescriptionVersions.AsNoTracking()
            .Where(version => version.NeedsReview &&
                              version.WorkflowStatus != EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.Approved &&
                              version.WorkflowStatus != EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.Archived);
        if (departmentIds is not null)
        {
            versionQuery = versionQuery.Where(version => departmentIds.Contains(version.DepartmentId));
        }

        var versions = await versionQuery
            .Include(version => version.Tasks)
            .Include(version => version.Skills)
            .OrderBy(version => version.DepartmentId)
            .ThenBy(version => version.PersonName)
            .ThenBy(version => version.Id)
            .ToArrayAsync(cancellationToken);
        var taskIds = versions.SelectMany(version => version.Tasks)
            .Select(task => task.TaskCatalogItemId)
            .Distinct()
            .ToArray();
        var taskCatalog = await context.TaskCatalogItems.AsNoTracking()
            .Include(task => task.RequiredSkills)
            .Where(task => taskIds.Contains(task.Id))
            .ToDictionaryAsync(task => task.Id, cancellationToken);
        var missingSkillIds = taskCatalog.Values
            .SelectMany(task => task.RequiredSkillIds)
            .Distinct()
            .ToArray();
        var skillNames = await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => missingSkillIds.Contains(skill.Id))
            .ToDictionaryAsync(skill => skill.Id, skill => skill.Name, cancellationToken);
        var departmentNames = await context.Departments.AsNoTracking()
            .Where(department => departmentIds == null || departmentIds.Contains(department.Id))
            .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);

        return versions
            .SelectMany(version => version.Tasks.SelectMany(task =>
            {
                if (!taskCatalog.TryGetValue(task.TaskCatalogItemId, out var catalogTask))
                {
                    return [];
                }

                var selectedSkillIds = version.SkillIds.ToHashSet();
                return catalogTask.RequiredSkillIds
                    .Where(skillId => !selectedSkillIds.Contains(skillId))
                    .Select(skillId => new JobDescriptionReviewWarning(
                        version.Id,
                        version.DepartmentId,
                        departmentNames.GetValueOrDefault(version.DepartmentId, "بخش نامشخص"),
                        version.PersonName,
                        task.Title,
                        skillNames.GetValueOrDefault(skillId, $"شناسه {skillId}")));
            }))
            .ToArray();
    }

    public async Task<JobDescriptionComparisonVersions?> GetAsync(long versionId, CancellationToken cancellationToken)
    {
        var current = await WithDetails(context.JobDescriptionVersions.AsNoTracking())
            .SingleOrDefaultAsync(item => item.Id == versionId, cancellationToken);
        if (current is null || current.JobDescriptionRecordId is null)
        {
            return current is null ? null : new(current, null);
        }

        var previous = await WithDetails(context.JobDescriptionVersions.AsNoTracking())
            .Where(item => item.JobDescriptionRecordId == current.JobDescriptionRecordId && item.Id != current.Id)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new(current, previous);
    }

    public void AddRecord(JobDescriptionRecord record) => context.JobDescriptionRecords.Add(record);

    public void AddVersion(JobDescriptionVersion version) => context.JobDescriptionVersions.Add(version);

    public async Task<bool> AreValidSelectionsAsync(
        long departmentId,
        IReadOnlyCollection<long> skillIds,
        IReadOnlyCollection<long> taskCatalogItemIds,
        CancellationToken cancellationToken)
    {
        var availableSkillCount = await context.SkillCatalogItems
            .Where(skill => skill.IsActive && (skill.DepartmentId == null || skill.DepartmentId == departmentId) && skillIds.Contains(skill.Id))
            .Select(skill => skill.Id)
            .Distinct()
            .CountAsync(cancellationToken);
        var availableTaskCount = await context.TaskCatalogItems
            .Where(task => task.IsActive && task.DepartmentId == departmentId && taskCatalogItemIds.Contains(task.Id))
            .Select(task => task.Id)
            .Distinct()
            .CountAsync(cancellationToken);
        return availableSkillCount == skillIds.Distinct().Count() &&
               availableTaskCount == taskCatalogItemIds.Distinct().Count();
    }

    public async Task<IReadOnlyList<string>> GetSkillNamesAsync(
        IReadOnlyCollection<long> skillIds,
        CancellationToken cancellationToken) =>
        await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => skillIds.Contains(skill.Id))
            .OrderBy(skill => skill.Name)
            .Select(skill => skill.Name)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, string>> GetSkillNameMapAsync(
        IReadOnlyCollection<long> skillIds,
        CancellationToken cancellationToken) =>
        await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => skillIds.Contains(skill.Id))
            .ToDictionaryAsync(skill => skill.Id, skill => skill.Name, cancellationToken);

    public async Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(
        IReadOnlyCollection<long> departmentIds,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var skills = await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => (includeInactive || skill.IsActive) && (skill.DepartmentId == null || departmentIds.Contains(skill.DepartmentId.Value)))
            .OrderBy(skill => skill.DepartmentId).ThenBy(skill => skill.Name)
            .Select(skill => new { skill.Id, skill.DepartmentId, skill.Name, skill.OwnerDepartmentId, skill.IsActive })
            .ToArrayAsync(cancellationToken);
        return await AddSkillUsageAsync(
            skills,
            skill => skill.Id,
            skill => skill.DepartmentId,
            skill => skill.Name,
            skill => skill.OwnerDepartmentId,
            skill => skill.IsActive,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(
        IReadOnlyCollection<long> departmentIds,
        long? departmentId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = context.TaskCatalogItems.AsNoTracking()
            .Where(task => (includeInactive || task.IsActive) && departmentIds.Contains(task.DepartmentId));
        if (departmentId is { } selectedDepartmentId)
        {
            query = query.Where(task => task.DepartmentId == selectedDepartmentId);
        }

        return await query.OrderBy(task => task.Title)
            .Select(task => new TaskCatalogListItem(task.Id, task.DepartmentId, task.Title, task.IsProject, task.IsActive, task.RequiredSkills.Select(skill => skill.SkillCatalogItemId).ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    public Task<SkillCatalogItem?> FindSkillByNameAsync(
        long? departmentId,
        string name,
        long? excludingId,
        CancellationToken cancellationToken) =>
        context.SkillCatalogItems.SingleOrDefaultAsync(
            skill => skill.DepartmentId == departmentId && skill.Name == name &&
                     (excludingId == null || skill.Id != excludingId), cancellationToken);

    public Task<TaskCatalogItem?> FindTaskByTitleAsync(
        long departmentId,
        string title,
        long? excludingId,
        CancellationToken cancellationToken) =>
        context.TaskCatalogItems.SingleOrDefaultAsync(
            task => task.DepartmentId == departmentId && task.Title == title &&
                    (excludingId == null || task.Id != excludingId), cancellationToken);

    public Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.TaskCatalogItems.Include(task => task.RequiredSkills).SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.SkillCatalogItems.SingleOrDefaultAsync(skill => skill.Id == id, cancellationToken);

    public async Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) =>
        await context.SkillCatalogItems.CountAsync(skill => skill.IsActive &&
            (skill.DepartmentId == null || skill.DepartmentId == departmentId) && skillIds.Contains(skill.Id), cancellationToken) == skillIds.Count;

    public void AddSkill(SkillCatalogItem skill) => context.SkillCatalogItems.Add(skill);

    public void AddTask(TaskCatalogItem task) => context.TaskCatalogItems.Add(task);

    public async Task<IReadOnlyList<SkillCatalogListItem>> ListPublicSkillsAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var skills = await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => (includeInactive || skill.IsActive) && skill.DepartmentId == null)
            .OrderBy(skill => skill.Name)
            .Select(skill => new { skill.Id, skill.DepartmentId, skill.Name, skill.OwnerDepartmentId, skill.IsActive })
            .ToArrayAsync(cancellationToken);
        return await AddSkillUsageAsync(
            skills,
            skill => skill.Id,
            skill => skill.DepartmentId,
            skill => skill.Name,
            skill => skill.OwnerDepartmentId,
            skill => skill.IsActive,
            cancellationToken);
    }

    public Task<SkillCatalogItem?> GetPublicSkillForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.SkillCatalogItems.SingleOrDefaultAsync(skill => skill.Id == id && skill.DepartmentId == null, cancellationToken);

    public async Task<(SkillCatalogItem Source, SkillCatalogItem Target)?> GetPublicSkillPairForMergeAsync(
        long sourceSkillId,
        long survivingSkillId,
        CancellationToken cancellationToken)
    {
        var skills = await context.SkillCatalogItems
            .Where(skill => (skill.Id == sourceSkillId || skill.Id == survivingSkillId) && skill.DepartmentId == null)
            .ToArrayAsync(cancellationToken);
        var source = skills.SingleOrDefault(skill => skill.Id == sourceSkillId);
        var target = skills.SingleOrDefault(skill => skill.Id == survivingSkillId);
        return source is null || target is null ? null : (source, target);
    }

    public async Task MergePublicSkillReferencesAsync(
        long sourceSkillId,
        long survivingSkillId,
        CancellationToken cancellationToken)
    {
        var versionLinks = await context.JobDescriptionVersionSkills
            .Where(link => link.SkillCatalogItemId == sourceSkillId)
            .ToArrayAsync(cancellationToken);
        var versionTargetIds = await context.JobDescriptionVersionSkills
            .Where(link => link.SkillCatalogItemId == survivingSkillId)
            .Select(link => link.JobDescriptionVersionId)
            .ToHashSetAsync(cancellationToken);
        context.JobDescriptionVersionSkills.RemoveRange(versionLinks);
        context.JobDescriptionVersionSkills.AddRange(versionLinks
            .Where(link => !versionTargetIds.Contains(link.JobDescriptionVersionId))
            .Select(link => new JobDescriptionVersionSkill(link.JobDescriptionVersionId, survivingSkillId)));

        var taskLinks = await context.TaskCatalogRequiredSkills
            .Where(link => link.SkillCatalogItemId == sourceSkillId)
            .ToArrayAsync(cancellationToken);
        var taskTargetIds = await context.TaskCatalogRequiredSkills
            .Where(link => link.SkillCatalogItemId == survivingSkillId)
            .Select(link => link.TaskCatalogItemId)
            .ToHashSetAsync(cancellationToken);
        context.TaskCatalogRequiredSkills.RemoveRange(taskLinks);
        context.TaskCatalogRequiredSkills.AddRange(taskLinks
            .Where(link => !taskTargetIds.Contains(link.TaskCatalogItemId))
            .Select(link => new TaskCatalogRequiredSkill(link.TaskCatalogItemId, survivingSkillId)));
    }

    public async Task<IReadOnlyCollection<long>> GetSkillUsageDepartmentIdsAsync(long skillId, CancellationToken cancellationToken) =>
        await context.JobDescriptionVersionSkills.AsNoTracking()
            .Where(link => link.SkillCatalogItemId == skillId)
            .Join(context.JobDescriptionVersions.AsNoTracking(), link => link.JobDescriptionVersionId, version => version.Id, (_, version) => version.DepartmentId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyList<SkillCatalogListItem>> AddSkillUsageAsync<T>(
        IReadOnlyCollection<T> skills,
        Func<T, long> idSelector,
        Func<T, long?> departmentIdSelector,
        Func<T, string> nameSelector,
        Func<T, long?> ownerDepartmentIdSelector,
        Func<T, bool> isActiveSelector,
        CancellationToken cancellationToken)
    {
        var skillIds = skills.Select(idSelector).ToArray();
        var usages = await context.JobDescriptionVersionSkills.AsNoTracking()
            .Where(link => skillIds.Contains(link.SkillCatalogItemId))
            .Join(context.JobDescriptionVersions.AsNoTracking(), link => link.JobDescriptionVersionId, version => version.Id, (link, version) => new { link.SkillCatalogItemId, version.DepartmentId })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        return skills.Select(skill => new SkillCatalogListItem(
            idSelector(skill),
            departmentIdSelector(skill),
            nameSelector(skill),
            ownerDepartmentIdSelector(skill),
            isActiveSelector(skill),
            usages.Where(usage => usage.SkillCatalogItemId == idSelector(skill)).Select(usage => usage.DepartmentId).ToArray())).ToArray();
    }

    private static IQueryable<JobDescriptionVersion> WithDetails(IQueryable<JobDescriptionVersion> query) => query
        .Include(item => item.JobDescriptionRecord)
        .Include(item => item.Tasks)
        .Include(item => item.Skills)
        .Include(item => item.UnresolvedSkills)
        .Include(item => item.UnresolvedTasks);
}
