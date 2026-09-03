using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using EosDashboards.Infrastructure.Persistence;
using EosDashboards.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EosDashboards.IntegrationTests.Database;

[Collection(SqlServerDatabaseCollection.Name)]
public sealed class RepositoryTests(SqlServerDatabaseFixture database)
{
    private static readonly DateTimeOffset TestNow = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UserRepository_OrganizationalLookupPersistsProvisioningMutations()
    {
        await using var context = database.CreateDbContext();
        var users = new UserRepository(context);
        var roles = new RoleRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var suffix = Guid.NewGuid().ToString("N");
        var role = Role.Create($"role-{suffix}", "نقش آزمایشی", true, TestNow);
        var user = CreateUser(suffix);
        roles.Add(role);
        users.Add(user);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var found = await users.FindByOrganizationalIdAsync(
            $"org-{suffix}",
            CancellationToken.None);
        found!.UpdateProfile(
            $"updated-account-{suffix}",
            "Updated",
            "Profile",
            "updated-protected-test-value",
            "***1111",
            TestNow.AddMinutes(1));
        found.AssignRole(role.Id);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await using var verificationContext = database.CreateDbContext();
        var persisted = await verificationContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.UserRoles)
            .SingleAsync(candidate => candidate.Id == user.Id, CancellationToken.None);
        Assert.Equal($"updated-account-{suffix}", persisted.AccountName);
        Assert.Equal("Updated", persisted.FirstName);
        Assert.Equal("Profile", persisted.LastName);
        Assert.Equal("updated-protected-test-value", persisted.ProtectedMobileNumber);
        Assert.Equal("***1111", persisted.MaskedMobileNumber);
        Assert.Contains(persisted.UserRoles, assignment => assignment.RoleId == role.Id);
    }

    [Fact]
    public async Task OtpChallengeRepository_ReturnsLatestActiveChallengeTrackedForMutation()
    {
        await using var context = database.CreateDbContext();
        var user = await AddUserAsync(context);
        var repository = new OtpChallengeRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var earlier = CreateChallenge(user.Id, TestNow, "earlier");
        var latest = CreateChallenge(user.Id, TestNow.AddMinutes(1), "latest");
        earlier.MarkSent();
        latest.MarkSent();
        repository.Add(earlier);
        repository.Add(latest);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var found = await repository.FindLatestActiveAsync(
            user.Id,
            CancellationToken.None);
        found!.Supersede();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var persistedStatus = await context.Set<OtpChallenge>()
            .AsNoTracking()
            .Where(challenge => challenge.Id == latest.Id)
            .Select(challenge => challenge.Status)
            .SingleAsync(CancellationToken.None);
        Assert.Equal(OtpChallengeStatus.Superseded, persistedStatus);
    }

    [Fact]
    public async Task UserSessionRepository_FindsRefreshHashTrackedForRotation()
    {
        await using var context = database.CreateDbContext();
        var user = await AddUserAsync(context);
        var repository = new UserSessionRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var originalHash = new string('a', 64);
        var replacementHash = new string('b', 64);
        var session = UserSession.Create(user.Id, originalHash, TestNow);
        repository.Add(session);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var found = await repository.FindByRefreshHashAsync(
            originalHash,
            CancellationToken.None);
        found!.Rotate(replacementHash, TestNow.AddMinutes(1));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.NotNull(await repository.FindByRefreshHashAsync(
            replacementHash,
            CancellationToken.None));
    }

    [Fact]
    public async Task ExternalIdentityLinkRepository_ReturnsTrackedPendingLinkForSubjectBinding()
    {
        await using var context = database.CreateDbContext();
        var user = await AddUserAsync(context);
        var repository = new ExternalIdentityLinkRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var suffix = Guid.NewGuid().ToString("N");
        repository.Add(ExternalIdentityLink.CreatePending(
            user.Id,
            ExternalIdentityProvider.Google,
            $"person-{suffix}@example.com",
            TestNow));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var found = await repository.FindPendingByProviderEmailAsync(
            ExternalIdentityProvider.Google,
            $"PERSON-{suffix}@EXAMPLE.COM",
            CancellationToken.None);
        found!.BindSubject($"subject-{suffix}", TestNow.AddMinutes(1));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.NotNull(await repository.FindByProviderSubjectAsync(
            ExternalIdentityProvider.Google,
            $"subject-{suffix}",
            CancellationToken.None));
    }

    [Fact]
    public async Task RoleAndPreferenceRepositories_ReturnNoTrackingReads()
    {
        await using var context = database.CreateDbContext();
        var user = await AddUserAsync(context);
        var suffix = Guid.NewGuid().ToString("N");
        var role = Role.Create($"role-{suffix}", "نقش آزمایشی", false, TestNow);
        var preference = UserPreference.Create(user.Id, "system", "navy-teal", false, TestNow);
        var roles = new RoleRepository(context);
        var preferences = new UserPreferenceRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        roles.Add(role);
        preferences.Add(preference);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var foundRole = await roles.FindByCodeAsync(role.Code, CancellationToken.None);
        var foundPreference = await preferences.FindByUserIdAsync(
            user.Id,
            CancellationToken.None);

        Assert.Equal(role.Id, foundRole!.Id);
        Assert.Equal(preference.Id, foundPreference!.Id);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task AuditWriter_PersistsSafeRecordWithUtcTimestamp()
    {
        await using var context = database.CreateDbContext();
        var writer = new AuditWriter(context, new FixedClock(TestNow));
        var unitOfWork = new EfUnitOfWork(context);
        var record = new AuditRecord(
            null,
            null,
            "authentication.test",
            true,
            "trace-test",
            new Dictionary<string, string> { ["reason"] = "synthetic" });

        await writer.WriteAsync(record, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var audit = await context.Set<AuditLog>()
            .AsNoTracking()
            .SingleAsync(
                item => item.TraceId == "trace-test",
                CancellationToken.None);
        Assert.Equal(TestNow, audit.OccurredAtUtc);
        Assert.Equal("authentication.test", audit.EventCode);
        using var metadata = JsonDocument.Parse(audit.SafeMetadata!);
        Assert.Equal("synthetic", metadata.RootElement.GetProperty("reason").GetString());
    }

    private static User CreateUser(string suffix) => User.Create(
        $"org-{suffix}",
        $"account-{suffix}",
        "Test",
        "User",
        "protected-test-value",
        "***0000",
        TestNow);

    private static OtpChallenge CreateChallenge(long userId, DateTimeOffset createdAtUtc, string label) =>
        OtpChallenge.Create(
            userId,
            $"challenge-{label}-{Guid.NewGuid():N}",
            new string('a', 64),
            createdAtUtc,
            createdAtUtc.AddMinutes(5));

    private static async Task<User> AddUserAsync(Microsoft.EntityFrameworkCore.DbContext context)
    {
        var user = CreateUser(Guid.NewGuid().ToString("N"));
        context.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();
        return user;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
