using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<User?> GetForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<int> CountActiveWithRoleAsync(long roleId, CancellationToken cancellationToken);

    void Add(User user);
}
