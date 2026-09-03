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
        DateTime now,
        CancellationToken cancellationToken) =>
        await context.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAt == null &&
                session.CreatedAt <= now &&
                now < session.ExpiresAt)
            .ToArrayAsync(cancellationToken);

    public void Add(UserSession session) => context.UserSessions.Add(session);
}
