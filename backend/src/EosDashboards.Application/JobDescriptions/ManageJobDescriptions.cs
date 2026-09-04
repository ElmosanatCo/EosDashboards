using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.JobDescriptions;

public sealed class ManageJobDescriptions(
    IClock clock,
    IJobDescriptionRepository repository,
    IJobDescriptionScope scope,
    IJobDescriptionCatalogReader catalog,
    IJobDescriptionWorkbookGenerator generator,
    IUnitOfWork unitOfWork)
{
    public async Task<JobDescriptionVersion?> GetForAuthorizedReadAsync(long actorUserId, long versionId, CancellationToken cancellationToken)
    {
        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null) return null;
        if (await scope.CanManageDepartmentAsync(actorUserId, version.DepartmentId, cancellationToken) ||
            await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return version;
        }
        return null;
    }

    public async Task<JobDescriptionOperationResult> ReviseAsync(
        long actorUserId,
        long versionId,
        CreateJobDescriptionCommand command,
        CancellationToken cancellationToken)
    {
        var previous = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (previous is null) return new(JobDescriptionOperationStatus.NotFound);
        if (!await scope.CanManageDepartmentAsync(actorUserId, previous.DepartmentId, cancellationToken))
            return new(JobDescriptionOperationStatus.Forbidden);
        if (string.IsNullOrWhiteSpace(command.PersonnelCode))
            return new(JobDescriptionOperationStatus.Invalid);
        if (command.Tasks.Any(task => task.WeeklyHours is null or < 0 or > 168))
            return new(JobDescriptionOperationStatus.Invalid);
        if (!await scope.CanManageDepartmentAsync(actorUserId, command.DepartmentId, cancellationToken))
            return new(JobDescriptionOperationStatus.Forbidden);
        if (!await catalog.AreValidSelectionsAsync(command.DepartmentId, command.SkillIds, command.Tasks.Select(task => task.TaskCatalogItemId).ToArray(), cancellationToken))
            return new(JobDescriptionOperationStatus.Invalid);

        try
        {
            var version = JobDescriptionVersion.Create(
                command.PersonName, command.DepartmentId, command.PersonnelCode,
                command.Education, command.FieldOfStudy, command.MinimumExperience,
                command.SkillIds,
                command.Tasks.Select(task => JobDescriptionTask.Create(task.TaskCatalogItemId, task.Title, task.Description, task.StartDate, task.EndDate, task.SortOrder, task.WeeklyHours)),
                clock.Now,
                previous.JobDescriptionRecordId);
            if (previous.JobDescriptionRecord is not null &&
                previous.DepartmentId != command.DepartmentId)
            {
                previous.JobDescriptionRecord.MoveToDepartment(command.DepartmentId, clock.Now);
            }
            if (previous.WorkflowStatus == JobDescriptionWorkflowStatus.Approved)
                previous.Archive(clock.Now);
            repository.AddVersion(version);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            version.SetExcelArtifact(generator.Generate(version, DateOnly.FromDateTime(clock.Now)), $"شرح-وظایف-{version.PersonName}.xlsx", clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (ArgumentException)
        {
            return new(JobDescriptionOperationStatus.Invalid);
        }
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>?> ListAsync(
        long actorUserId,
        IReadOnlyCollection<long> managedDepartmentIds,
        long? departmentId,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || managedDepartmentIds is null ||
            (departmentId is not null && !managedDepartmentIds.Contains(departmentId.Value)))
        {
            return null;
        }

        return await repository.ListAsync(managedDepartmentIds, departmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<JobDescriptionListItem>?> ListForHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken)) return null;
        return await repository.ListForHumanResourcesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<SkillCatalogListItem> Skills, IReadOnlyList<TaskCatalogListItem> Tasks)?> ListCatalogAsync(
        long actorUserId,
        IReadOnlyCollection<long> managedDepartmentIds,
        long? departmentId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || managedDepartmentIds.Count == 0 ||
            (departmentId is not null && !managedDepartmentIds.Contains(departmentId.Value)))
        {
            return null;
        }

        return (
            await catalog.ListSkillsAsync(
                departmentId is { } selectedDepartmentId ? [selectedDepartmentId] : managedDepartmentIds,
                includeInactive,
                cancellationToken),
            await catalog.ListTasksAsync(managedDepartmentIds, departmentId, includeInactive, cancellationToken));
    }

    public async Task<JobDescriptionOperationResult> CreateAsync(
        long actorUserId,
        CreateJobDescriptionCommand command,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || command is null || command.DepartmentId <= 0 ||
            command.SkillIds is null || command.Tasks is null ||
            string.IsNullOrWhiteSpace(command.PersonName) ||
            string.IsNullOrWhiteSpace(command.PersonnelCode) ||
            command.Tasks.Any(task => task.WeeklyHours is null or < 0 or > 168))
        {
            return new(JobDescriptionOperationStatus.Invalid);
        }

        if (!await scope.CanManageDepartmentAsync(actorUserId, command.DepartmentId, cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Forbidden);
        }

        if (!await catalog.AreValidSelectionsAsync(
                command.DepartmentId,
                command.SkillIds,
                command.Tasks.Select(task => task.TaskCatalogItemId).ToArray(),
                cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Invalid);
        }

        try
        {
            var record = JobDescriptionRecord.Create(command.DepartmentId, command.PersonName, clock.Now);
            repository.AddRecord(record);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var tasks = command.Tasks.Select(task => JobDescriptionTask.Create(
                task.TaskCatalogItemId,
                task.Title,
                task.Description,
                task.StartDate,
                task.EndDate,
                task.SortOrder,
                task.WeeklyHours));
            var version = JobDescriptionVersion.Create(
                command.PersonName,
                command.DepartmentId,
                command.PersonnelCode,
                command.Education,
                command.FieldOfStudy,
                command.MinimumExperience,
                command.SkillIds,
                tasks,
                clock.Now,
                record.Id);
            repository.AddVersion(version);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            version.SetExcelArtifact(generator.Generate(version, DateOnly.FromDateTime(clock.Now)), $"شرح-وظایف-{version.PersonName}.xlsx", clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (ArgumentException)
        {
            return new(JobDescriptionOperationStatus.Invalid);
        }
    }

    public async Task<JobDescriptionOperationResult> ApproveByDepartmentManagerAsync(
        long actorUserId,
        long versionId,
        CancellationToken cancellationToken)
    {
        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null)
        {
            return new(JobDescriptionOperationStatus.NotFound);
        }

        if (!await scope.CanManageDepartmentAsync(actorUserId, version.DepartmentId, cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Forbidden);
        }

        try
        {
            version.ApproveByDepartmentManager(clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (InvalidOperationException)
        {
            return version.QualityStatus == JobDescriptionQualityStatus.Incomplete
                ? new(JobDescriptionOperationStatus.Incomplete, version)
                : new(JobDescriptionOperationStatus.Conflict);
        }
    }

    public async Task<JobDescriptionOperationResult> ApproveByHumanResourcesAsync(
        long actorUserId,
        long versionId,
        CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Forbidden);
        }

        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null)
        {
            return new(JobDescriptionOperationStatus.NotFound);
        }

        try
        {
            version.ApproveByHumanResources(clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (InvalidOperationException)
        {
            return new(JobDescriptionOperationStatus.Conflict);
        }
    }

    public async Task<JobDescriptionOperationResult> RejectByHumanResourcesAsync(
        long actorUserId,
        long versionId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Forbidden);
        }

        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null)
        {
            return new(JobDescriptionOperationStatus.NotFound);
        }

        try
        {
            version.RejectByHumanResources(reason, clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (ArgumentException)
        {
            return new(JobDescriptionOperationStatus.Invalid);
        }
        catch (InvalidOperationException)
        {
            return new(JobDescriptionOperationStatus.Conflict);
        }
    }

    public async Task<JobDescriptionOperationResult> ArchiveAsync(
        long actorUserId,
        long versionId,
        CancellationToken cancellationToken)
    {
        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null)
        {
            return new(JobDescriptionOperationStatus.NotFound);
        }

        if (!await scope.CanManageDepartmentAsync(actorUserId, version.DepartmentId, cancellationToken))
        {
            return new(JobDescriptionOperationStatus.Forbidden);
        }

        try
        {
            version.Archive(clock.Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(JobDescriptionOperationStatus.Succeeded, version);
        }
        catch (InvalidOperationException)
        {
            return new(JobDescriptionOperationStatus.Conflict);
        }
    }
}
