using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionAnalysisReader(EosDashboardDbContext context) : IJobDescriptionAnalysisReader
{
    public async Task<IReadOnlyList<TaskCatalogItem>> GetTasksAsync(long departmentId, IReadOnlyCollection<long> taskIds, CancellationToken cancellationToken) =>
        await context.TaskCatalogItems.AsNoTracking()
            .Include(task => task.RequiredSkills)
            .Where(task => task.DepartmentId == departmentId && taskIds.Contains(task.Id))
            .ToArrayAsync(cancellationToken);
}
