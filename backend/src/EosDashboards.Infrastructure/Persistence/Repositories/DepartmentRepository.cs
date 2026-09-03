using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository(EosDashboardDbContext context) : IDepartmentRepository
{
    public Task<Department?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Departments.SingleOrDefaultAsync(department => department.Name == name, cancellationToken);

    public Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        context.Departments.AsNoTracking().SingleOrDefaultAsync(department => department.Id == id, cancellationToken);
}
