using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<Department?> GetForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<int> CountChildrenAsync(long id, CancellationToken cancellationToken);

    Task<int> CountAssignedUsersAsync(long id, CancellationToken cancellationToken);

    void Add(Department department);

    void Remove(Department department);
}
