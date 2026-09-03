using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.IntegrationTests.Database;

[Collection(SqlServerDatabaseCollection.Name)]
public sealed class DatabaseConstraintTests(SqlServerDatabaseFixture database)
{
    private static readonly DateTime TestNow = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task Otp_challenge_timestamps_use_local_millisecond_datetime2_columns()
    {
        // Break caught: storing a local application time as an offset-bearing or sub-millisecond SQL value.
        await using var context = database.CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(OtpChallenge))!;

        Assert.Equal(
            "datetime2(3)",
            entityType.FindProperty(nameof(OtpChallenge.CreatedAt))!.GetColumnType());
        Assert.Null(entityType.FindProperty("CreatedAtUtc"));
    }

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
    public async Task Departments_RejectDuplicateNames()
    {
        // Break caught: making a user department selection ambiguous through duplicate names.
        await using var context = database.CreateDbContext();
        var name = $"واحد آزمایشی {Guid.NewGuid():N}";
        context.Departments.Add(Department.CreateRoot(name, TestNow));
        context.Departments.Add(Department.CreateRoot(name, TestNow));

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Users_RejectStaleConcurrentProfileUpdates()
    {
        // Break caught: allowing one administrator to overwrite another administrator's user edit.
        long userId;
        await using (var seedContext = database.CreateDbContext())
        {
            var user = await AddUserAsync(seedContext);
            userId = user.Id;
        }

        await using var firstContext = database.CreateDbContext();
        await using var staleContext = database.CreateDbContext();
        var first = await firstContext.Users.SingleAsync(user => user.Id == userId, CancellationToken.None);
        var stale = await staleContext.Users.SingleAsync(user => user.Id == userId, CancellationToken.None);

        first.UpdateOrganizationalId($"first-{Guid.NewGuid():N}", TestNow.AddMinutes(1));
        await firstContext.SaveChangesAsync(CancellationToken.None);
        stale.UpdateOrganizationalId($"stale-{Guid.NewGuid():N}", TestNow.AddMinutes(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => staleContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ExternalIdentityLinks_RejectDuplicateGoogleEmailOrSubject()
    {
        await using var context = database.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var firstUser = await AddUserAsync(context);
        var secondUser = await AddUserAsync(context);
        var first = ExternalIdentityLink.CreatePending(
            firstUser.Id,
            ExternalIdentityProvider.Google,
            $"person-{suffix}@example.com",
            TestNow);
        first.BindSubject($"subject-{suffix}", TestNow);
        var duplicate = ExternalIdentityLink.CreatePending(
            secondUser.Id,
            ExternalIdentityProvider.Google,
            $"person-{suffix}@example.com",
            TestNow);
        duplicate.BindSubject($"subject-{suffix}", TestNow);
        context.Add(first);
        context.Add(duplicate);

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
        1,
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
