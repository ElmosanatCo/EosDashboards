using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.IntegrationTests.Database;

[Collection(SqlServerDatabaseCollection.Name)]
public sealed class DatabaseConstraintTests(SqlServerDatabaseFixture database)
{
    private static readonly DateTimeOffset TestNow = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Users_RejectDuplicateOrganizationalIdentifiers()
    {
        await using var context = database.CreateDbContext();
        var organizationalId = $"duplicate-org-{Guid.NewGuid():N}";
        context.Users.Add(CreateUser(organizationalId, Guid.NewGuid().ToString("N")));
        context.Users.Add(CreateUser(organizationalId, Guid.NewGuid().ToString("N")));

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OtpChallenges_RejectStaleConcurrentUpdates()
    {
        var publicToken = $"concurrent-challenge-{Guid.NewGuid():N}";
        long challengeId;
        await using (var seedContext = database.CreateDbContext())
        {
            var user = await AddUserAsync(seedContext);
            var challenge = OtpChallenge.Create(
                user.Id,
                publicToken,
                new string('a', 64),
                TestNow,
                TestNow.AddMinutes(5));
            challenge.MarkSent();
            seedContext.OtpChallenges.Add(challenge);
            await seedContext.SaveChangesAsync(CancellationToken.None);
            challengeId = challenge.Id;
        }

        await using var firstContext = database.CreateDbContext();
        await using var staleContext = database.CreateDbContext();
        var first = await firstContext.OtpChallenges.SingleAsync(
            challenge => challenge.Id == challengeId,
            CancellationToken.None);
        var stale = await staleContext.OtpChallenges.SingleAsync(
            challenge => challenge.Id == challengeId,
            CancellationToken.None);

        first.Supersede();
        await firstContext.SaveChangesAsync(CancellationToken.None);
        stale.MarkSendFailed();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => staleContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UserSessions_RejectStaleConcurrentUpdates()
    {
        var hashSuffix = Guid.NewGuid().ToString("N");
        long sessionId;
        await using (var seedContext = database.CreateDbContext())
        {
            var user = await AddUserAsync(seedContext);
            var session = UserSession.Create(user.Id, $"original-{hashSuffix}", TestNow);
            seedContext.UserSessions.Add(session);
            await seedContext.SaveChangesAsync(CancellationToken.None);
            sessionId = session.Id;
        }

        await using var firstContext = database.CreateDbContext();
        await using var staleContext = database.CreateDbContext();
        var first = await firstContext.UserSessions.SingleAsync(
            session => session.Id == sessionId,
            CancellationToken.None);
        var stale = await staleContext.UserSessions.SingleAsync(
            session => session.Id == sessionId,
            CancellationToken.None);

        first.Rotate($"first-{hashSuffix}", TestNow.AddMinutes(1));
        await firstContext.SaveChangesAsync(CancellationToken.None);
        stale.Rotate($"stale-{hashSuffix}", TestNow.AddMinutes(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => staleContext.SaveChangesAsync(CancellationToken.None));
    }

    private static User CreateUser(string organizationalId, string suffix) => User.Create(
        organizationalId,
        $"account-{suffix}",
        "Test",
        "User",
        "protected-test-value",
        "***0000",
        TestNow);

    private static async Task<User> AddUserAsync(EosDashboards.Infrastructure.Persistence.EosDashboardDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = CreateUser($"org-{suffix}", suffix);
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        return user;
    }
}
