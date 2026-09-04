using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.JobDescriptions;

public sealed class ManageCatalog(
    IClock clock,
    IJobDescriptionScope scope,
    IJobDescriptionCatalogReader catalog,
    IHumanResourcesCatalogReader humanResourcesCatalog,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<SkillCatalogListItem>?> ListPublicSkillsAsync(long actorUserId, CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken)) return null;
        return await humanResourcesCatalog.ListPublicSkillsAsync(cancellationToken);
    }

    public async Task<CatalogOperationResult> RenamePublicSkillAsync(long actorUserId, long skillId, string name, CancellationToken cancellationToken)
    {
        var skill = await humanResourcesCatalog.GetPublicSkillForUpdateAsync(skillId, cancellationToken);
        if (skill is null) return new(CatalogOperationStatus.NotFound);
        if (!await CanManagePublicSkillAsync(actorUserId, skill, cancellationToken) &&
            !await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }
        try
        {
            skill.Rename(name, clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(CatalogOperationStatus.Succeeded, skill.Id);
        }
        catch (ArgumentException) { return new(CatalogOperationStatus.Invalid); }
    }

    public async Task<CatalogOperationResult> DeactivatePublicSkillAsync(long actorUserId, long skillId, CancellationToken cancellationToken)
    {
        var skill = await humanResourcesCatalog.GetPublicSkillForUpdateAsync(skillId, cancellationToken);
        if (skill is null) return new(CatalogOperationStatus.NotFound);
        if (!await CanManagePublicSkillAsync(actorUserId, skill, cancellationToken) &&
            !await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }
        skill.Deactivate(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(CatalogOperationStatus.Succeeded, skill.Id);
    }

    public async Task<CatalogOperationResult> CreatePublicSkillAsync(long actorUserId, CreatePublicSkillCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.OwnerDepartmentId <= 0 || string.IsNullOrWhiteSpace(command.Name) ||
            !await scope.CanManageDepartmentAsync(actorUserId, command.OwnerDepartmentId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }

        try
        {
            var skill = SkillCatalogItem.CreatePublic(command.OwnerDepartmentId, command.Name, clock.Now);
            catalog.AddSkill(skill);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(CatalogOperationStatus.Succeeded, skill.Id);
        }
        catch (ArgumentException)
        {
            return new(CatalogOperationStatus.Invalid);
        }
    }
    public async Task<CatalogOperationResult> CreateSkillAsync(long actorUserId, CreateSkillCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.DepartmentId <= 0 || string.IsNullOrWhiteSpace(command.Name) ||
            !await scope.CanManageDepartmentAsync(actorUserId, command.DepartmentId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }

        try
        {
            var skill = SkillCatalogItem.Create(command.DepartmentId, command.Name, clock.Now);
            catalog.AddSkill(skill);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(CatalogOperationStatus.Succeeded, skill.Id);
        }
        catch (ArgumentException)
        {
            return new(CatalogOperationStatus.Invalid);
        }
    }

    public async Task<CatalogOperationResult> CreateTaskAsync(long actorUserId, CreateTaskCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.DepartmentId <= 0 || string.IsNullOrWhiteSpace(command.Title) ||
            !await scope.CanManageDepartmentAsync(actorUserId, command.DepartmentId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }

        try
        {
            var task = TaskCatalogItem.Create(command.DepartmentId, command.Title, command.IsProject, clock.Now);
            catalog.AddTask(task);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(CatalogOperationStatus.Succeeded, task.Id);
        }
        catch (ArgumentException)
        {
            return new(CatalogOperationStatus.Invalid);
        }
    }

    public async Task<CatalogOperationResult> SetRequiredSkillsAsync(long actorUserId, SetTaskRequiredSkillsCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.TaskId <= 0 || command.SkillIds is null)
        {
            return new(CatalogOperationStatus.Invalid);
        }

        var task = await catalog.GetTaskForUpdateAsync(command.TaskId, cancellationToken);
        if (task is null) return new(CatalogOperationStatus.NotFound);
        if (!await scope.CanManageDepartmentAsync(actorUserId, task.DepartmentId, cancellationToken))
        {
            return new(CatalogOperationStatus.Forbidden);
        }

        var skillIds = command.SkillIds.Distinct().ToArray();
        if (!await catalog.AreSkillsAvailableAsync(task.DepartmentId, skillIds, cancellationToken))
        {
            return new(CatalogOperationStatus.Invalid);
        }

        foreach (var skillId in task.RequiredSkillIds.Except(skillIds)) task.RemoveRequiredSkill(skillId);
        foreach (var skillId in skillIds) task.AddRequiredSkill(skillId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(CatalogOperationStatus.Succeeded, task.Id);
    }

    public async Task<CatalogOperationResult> RenameDepartmentSkillAsync(long actorUserId, long skillId, string name, CancellationToken cancellationToken)
    {
        var skill = await catalog.GetSkillForUpdateAsync(skillId, cancellationToken);
        if (skill is null) return new(CatalogOperationStatus.NotFound);
        if (skill.DepartmentId is null || !await scope.CanManageDepartmentAsync(actorUserId, skill.DepartmentId.Value, cancellationToken)) return new(CatalogOperationStatus.Forbidden);
        try { skill.Rename(name, clock.Now); await unitOfWork.SaveChangesAsync(cancellationToken); return new(CatalogOperationStatus.Succeeded, skill.Id); }
        catch (ArgumentException) { return new(CatalogOperationStatus.Invalid); }
    }

    public async Task<CatalogOperationResult> DeactivateDepartmentSkillAsync(long actorUserId, long skillId, CancellationToken cancellationToken)
    {
        var skill = await catalog.GetSkillForUpdateAsync(skillId, cancellationToken);
        if (skill is null) return new(CatalogOperationStatus.NotFound);
        if (skill.DepartmentId is null || !await scope.CanManageDepartmentAsync(actorUserId, skill.DepartmentId.Value, cancellationToken)) return new(CatalogOperationStatus.Forbidden);
        skill.Deactivate(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(CatalogOperationStatus.Succeeded, skill.Id);
    }

    public async Task<CatalogOperationResult> RenameDepartmentTaskAsync(long actorUserId, long taskId, string title, CancellationToken cancellationToken)
    {
        var task = await catalog.GetTaskForUpdateAsync(taskId, cancellationToken);
        if (task is null) return new(CatalogOperationStatus.NotFound);
        if (!await scope.CanManageDepartmentAsync(actorUserId, task.DepartmentId, cancellationToken)) return new(CatalogOperationStatus.Forbidden);
        try { task.Rename(title, clock.Now); await unitOfWork.SaveChangesAsync(cancellationToken); return new(CatalogOperationStatus.Succeeded, task.Id); }
        catch (ArgumentException) { return new(CatalogOperationStatus.Invalid); }
    }

    public async Task<CatalogOperationResult> DeactivateDepartmentTaskAsync(long actorUserId, long taskId, CancellationToken cancellationToken)
    {
        var task = await catalog.GetTaskForUpdateAsync(taskId, cancellationToken);
        if (task is null) return new(CatalogOperationStatus.NotFound);
        if (!await scope.CanManageDepartmentAsync(actorUserId, task.DepartmentId, cancellationToken)) return new(CatalogOperationStatus.Forbidden);
        task.Deactivate(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(CatalogOperationStatus.Succeeded, task.Id);
    }

    private async Task<bool> CanManagePublicSkillAsync(
        long actorUserId,
        SkillCatalogItem skill,
        CancellationToken cancellationToken)
    {
        if (skill.DepartmentId is not null || skill.OwnerDepartmentId is not { } ownerDepartmentId ||
            !await scope.CanManageDepartmentAsync(actorUserId, ownerDepartmentId, cancellationToken))
        {
            return false;
        }

        var usageDepartments = await catalog.GetSkillUsageDepartmentIdsAsync(skill.Id, cancellationToken);
        return usageDepartments.All(departmentId => departmentId == ownerDepartmentId);
    }
}
