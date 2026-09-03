using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
