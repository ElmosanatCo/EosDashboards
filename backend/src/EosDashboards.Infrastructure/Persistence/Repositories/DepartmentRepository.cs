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

    public Task<Department?> GetForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.Departments
            .Include(department => department.ParentDepartment)
            .SingleOrDefaultAsync(department => department.Id == id, cancellationToken);

    public Task<int> CountChildrenAsync(long id, CancellationToken cancellationToken) =>
        context.Departments.CountAsync(department => department.ParentDepartmentId == id, cancellationToken);

    public Task<int> CountAssignedUsersAsync(long id, CancellationToken cancellationToken) =>
        context.Users.CountAsync(user => user.DepartmentId == id, cancellationToken);

    public void Add(Department department) => context.Departments.Add(department);

    public void Remove(Department department) => context.Departments.Remove(department);
}
