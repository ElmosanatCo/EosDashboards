using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionRepository(EosDashboardDbContext context) : IJobDescriptionRepository, IJobDescriptionCatalogReader, IHumanResourcesCatalogReader
{
    public Task<JobDescriptionVersion?> GetForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.JobDescriptionVersions
            .Include(item => item.JobDescriptionRecord)
            .Include(item => item.Tasks)
            .Include(item => item.Skills)
            .Include(item => item.UnresolvedSkills)
            .Include(item => item.UnresolvedTasks)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

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
            .ToListAsync(cancellationToken);
        return versions.Select(item => new JobDescriptionListItem(
            item.Id,
            item.DepartmentId,
            item.PersonName,
            item.WorkflowStatus,
            item.QualityStatus,
            item.UpdatedAt)).ToArray();
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>> ListForHumanResourcesAsync(CancellationToken cancellationToken)
    {
        var versions = await context.JobDescriptionVersions.AsNoTracking()
            .Where(item => item.WorkflowStatus == EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus.UnderHumanResourcesReview)
            .OrderBy(item => item.UpdatedAt)
            .ToListAsync(cancellationToken);
        return versions.Select(item => new JobDescriptionListItem(
            item.Id, item.DepartmentId, item.PersonName, item.WorkflowStatus, item.QualityStatus, item.UpdatedAt)).ToArray();
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

    public async Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(
        IReadOnlyCollection<long> departmentIds,
        CancellationToken cancellationToken)
    {
        var skills = await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => skill.IsActive && (skill.DepartmentId == null || departmentIds.Contains(skill.DepartmentId.Value)))
            .OrderBy(skill => skill.DepartmentId).ThenBy(skill => skill.Name)
            .Select(skill => new { skill.Id, skill.DepartmentId, skill.Name, skill.OwnerDepartmentId })
            .ToArrayAsync(cancellationToken);
        return await AddSkillUsageAsync(
            skills,
            skill => skill.Id,
            skill => skill.DepartmentId,
            skill => skill.Name,
            skill => skill.OwnerDepartmentId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(
        IReadOnlyCollection<long> departmentIds,
        long? departmentId,
        CancellationToken cancellationToken)
    {
        var query = context.TaskCatalogItems.AsNoTracking()
            .Where(task => task.IsActive && departmentIds.Contains(task.DepartmentId));
        if (departmentId is { } selectedDepartmentId)
        {
            query = query.Where(task => task.DepartmentId == selectedDepartmentId);
        }

        return await query.OrderBy(task => task.Title)
            .Select(task => new TaskCatalogListItem(task.Id, task.DepartmentId, task.Title, task.IsProject, task.RequiredSkills.Select(skill => skill.SkillCatalogItemId).ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    public Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.TaskCatalogItems.Include(task => task.RequiredSkills).SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.SkillCatalogItems.SingleOrDefaultAsync(skill => skill.Id == id, cancellationToken);

    public async Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) =>
        await context.SkillCatalogItems.CountAsync(skill => skill.IsActive &&
            (skill.DepartmentId == null || skill.DepartmentId == departmentId) && skillIds.Contains(skill.Id), cancellationToken) == skillIds.Count;

    public void AddSkill(SkillCatalogItem skill) => context.SkillCatalogItems.Add(skill);

    public void AddTask(TaskCatalogItem task) => context.TaskCatalogItems.Add(task);

    public async Task<IReadOnlyList<SkillCatalogListItem>> ListPublicSkillsAsync(CancellationToken cancellationToken)
    {
        var skills = await context.SkillCatalogItems.AsNoTracking()
            .Where(skill => skill.IsActive && skill.DepartmentId == null)
            .OrderBy(skill => skill.Name)
            .Select(skill => new { skill.Id, skill.DepartmentId, skill.Name, skill.OwnerDepartmentId })
            .ToArrayAsync(cancellationToken);
        return await AddSkillUsageAsync(
            skills,
            skill => skill.Id,
            skill => skill.DepartmentId,
            skill => skill.Name,
            skill => skill.OwnerDepartmentId,
            cancellationToken);
    }

    public Task<SkillCatalogItem?> GetPublicSkillForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.SkillCatalogItems.SingleOrDefaultAsync(skill => skill.Id == id && skill.DepartmentId == null, cancellationToken);

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
            usages.Where(usage => usage.SkillCatalogItemId == idSelector(skill)).Select(usage => usage.DepartmentId).ToArray())).ToArray();
    }
}
