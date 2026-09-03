using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken);

    void Add(Role role);
}
