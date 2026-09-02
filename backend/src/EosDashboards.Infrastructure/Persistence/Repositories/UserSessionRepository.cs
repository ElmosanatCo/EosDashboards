using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository(EosDashboardDbContext context) : IUserSessionRepository
{
    public Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public Task<UserSession?> FindByRefreshHashAsync(string refreshHash, CancellationToken cancellationToken) =>
        context.UserSessions.SingleOrDefaultAsync(
            session => session.RefreshCredentialHash == refreshHash,
            cancellationToken);

    public async Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(
        long userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        await context.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAtUtc == null &&
                session.CreatedAtUtc <= nowUtc &&
                nowUtc < session.ExpiresAtUtc)
            .ToArrayAsync(cancellationToken);

    public void Add(UserSession session) => context.UserSessions.Add(session);
}
