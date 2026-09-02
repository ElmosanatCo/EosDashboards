using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class UserPreferenceRepository(EosDashboardDbContext context) : IUserPreferenceRepository
{
    public Task<UserPreference?> FindByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        context.UserPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(preference => preference.UserId == userId, cancellationToken);

    public Task<UserPreference?> GetForUpdateAsync(long userId, CancellationToken cancellationToken) =>
        context.UserPreferences
            .SingleOrDefaultAsync(preference => preference.UserId == userId, cancellationToken);

    public void Add(UserPreference preference) => context.UserPreferences.Add(preference);
}
