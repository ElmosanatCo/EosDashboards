using EosDashboards.Application.Abstractions;
using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class HumanResourcesDashboardReader(EosDashboardDbContext context, IClock clock) : IHumanResourcesDashboardReader
{
    public async Task<IReadOnlyList<ManagedDepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken) =>
        await context.Departments.AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new ManagedDepartmentListItem(department.Id, department.Name, false))
            .ToArrayAsync(cancellationToken);

    public async Task<HumanResourcesDashboardResult> GetAsync(
        long? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.JobDescriptionVersions.AsNoTracking()
            .Where(version => departmentId == null || version.DepartmentId == departmentId.Value);

        var versions = await query
            .Include(version => version.Tasks)
            .Include(version => version.Skills)
            .OrderByDescending(version => version.UpdatedAt)
            .ThenByDescending(version => version.Id)
            .ToArrayAsync(cancellationToken);

        var departments = await context.Departments.AsNoTracking()
            .Where(department => departmentId == null || department.Id == departmentId.Value)
            .OrderBy(department => department.Name)
            .Select(department => new { department.Id, department.Name })
            .ToArrayAsync(cancellationToken);
        var departmentNames = departments.ToDictionary(department => department.Id, department => department.Name);

        var activeVersions = versions.Where(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved).ToArray();
        var archivedVersions = versions.Where(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Archived).ToArray();
        var activeProjectQuery = context.JobDescriptionTasks
            .Join(context.JobDescriptionVersions,
                task => task.JobDescriptionVersionId,
                version => version.Id,
                (task, version) => new { task, version })
            .Join(context.TaskCatalogItems,
                item => item.task.TaskCatalogItemId,
                task => task.Id,
                (item, task) => new { item.task, item.version, catalogTask = task })
            .Where(item => (departmentId == null || item.version.DepartmentId == departmentId.Value) &&
                           item.version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved &&
                           item.catalogTask.IsProject &&
                           (item.task.EndDate == null || item.task.EndDate >= DateOnly.FromDateTime(clock.Now)));

        var metrics = new HumanResourcesMetricSet(
            versions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            activeVersions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            archivedVersions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            versions.Count(version => version.QualityStatus == JobDescriptionQualityStatus.Healthy),
            versions.Count(version => version.QualityStatus == JobDescriptionQualityStatus.Incomplete),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.PendingDataCompletion),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.PendingDepartmentApproval),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.UnderHumanResourcesReview),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Rejected),
            await activeProjectQuery.Select(item => item.task.TaskCatalogItemId).Distinct().CountAsync(cancellationToken),
            await activeProjectQuery.Select(item => item.version.Id).Distinct().CountAsync(cancellationToken));

        var summaries = departments
            .Select(department =>
            {
                var departmentVersions = versions.Where(version => version.DepartmentId == department.Id).ToArray();
                return new HumanResourcesChangeSummary(
                    department.Id,
                    department.Name,
                    departmentVersions.Length,
                    departmentVersions.Select(version => (DateTime?)version.UpdatedAt).Max());
            })
            .ToArray();

        var changes = versions
            .Select(version => new HumanResourcesChangeItem(
                version.Id,
                version.DepartmentId,
                departmentNames.GetValueOrDefault(version.DepartmentId, "بخش نامشخص"),
                version.PersonName,
                "job-description-version",
                version.UpdatedAt,
                null))
            .ToArray();
        var pageItems = changes.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        return new(metrics, summaries, pageItems, changes.Length, page, pageSize);
    }
}
