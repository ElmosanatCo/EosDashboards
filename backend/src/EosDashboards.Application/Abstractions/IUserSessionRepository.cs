using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<UserSession?> FindByRefreshHashAsync(string refreshHash, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(
        long userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    void Add(UserSession session);
}
