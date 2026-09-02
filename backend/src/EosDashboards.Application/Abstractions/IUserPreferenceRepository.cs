using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> FindByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<UserPreference?> GetForUpdateAsync(long userId, CancellationToken cancellationToken);

    void Add(UserPreference preference);
}
