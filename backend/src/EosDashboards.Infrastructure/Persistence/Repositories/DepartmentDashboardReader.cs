using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class DepartmentDashboardReader(EosDashboardDbContext context) : IDepartmentDashboardReader
{
    public async Task<DepartmentDashboardMetrics> GetAsync(
        IReadOnlyCollection<long> departmentIds,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var query = context.JobDescriptionVersions.AsNoTracking()
            .Where(version => departmentIds.Contains(version.DepartmentId));
        var versions = await query
            .Include(version => version.Tasks)
            .Include(version => version.Skills)
            .ToListAsync(cancellationToken);

        var activeVersions = versions.Where(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved).ToArray();
        var archivedVersions = versions.Where(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Archived).ToArray();
        var projects = context.JobDescriptionTasks
            .Join(context.JobDescriptionVersions,
                task => task.JobDescriptionVersionId,
                version => version.Id,
                (task, version) => new { task, version })
            .Join(context.TaskCatalogItems,
                item => item.task.TaskCatalogItemId,
                task => task.Id,
                (item, task) => new { item.task, item.version, catalogTask = task })
            .Where(item => departmentIds.Contains(item.version.DepartmentId) &&
                           item.version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved &&
                           item.catalogTask.IsProject &&
                           (item.task.EndDate == null || item.task.EndDate >= asOf));

        return new DepartmentDashboardMetrics(
            versions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            activeVersions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            archivedVersions.Select(version => version.JobDescriptionRecordId ?? version.Id).Distinct().Count(),
            versions.Count(version => version.QualityStatus == JobDescriptionQualityStatus.Healthy),
            versions.Count(version => version.QualityStatus == JobDescriptionQualityStatus.Incomplete),
            versions.Count(version => version.NeedsReview),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.PendingDataCompletion),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.PendingDepartmentApproval),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.UnderHumanResourcesReview),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Approved),
            versions.Count(version => version.WorkflowStatus == JobDescriptionWorkflowStatus.Rejected),
            await projects.Select(item => item.task.TaskCatalogItemId).Distinct().CountAsync(cancellationToken),
            await projects.Select(item => item.version.Id).Distinct().CountAsync(cancellationToken));
    }
}
