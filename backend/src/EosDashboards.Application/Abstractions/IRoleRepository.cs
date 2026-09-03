using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    void Add(Role role);
}
