using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<UserSession?> FindByRefreshHashAsync(string refreshHash, CancellationToken cancellationToken);

    void Add(UserSession session);
}
