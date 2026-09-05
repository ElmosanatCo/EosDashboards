using EosDashboards.Application.JobDescriptions;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionDepartmentReader(EosDashboardDbContext context) : IJobDescriptionDepartmentReader
{
    public Task<string?> GetNameAsync(long departmentId, CancellationToken cancellationToken) =>
        context.Departments.AsNoTracking()
            .Where(department => department.Id == departmentId)
            .Select(department => department.Name)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ManagedDepartmentListItem>> ListAsync(long ownDepartmentId, IReadOnlyCollection<long> departmentIds, CancellationToken cancellationToken) =>
        await context.Departments.AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .OrderBy(department => department.Id == ownDepartmentId ? 0 : 1)
            .ThenBy(department => department.Name)
            .Select(department => new ManagedDepartmentListItem(department.Id, department.Name, department.Id == ownDepartmentId))
            .ToArrayAsync(cancellationToken);
}
