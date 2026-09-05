using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Administration;
using EosDashboards.Domain.Authorization;
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
    private static readonly DateTime TestNow = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task JobDescriptionRepository_ListAsync_returns_latest_version_per_record()
    {
        await using var context = database.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var department = Department.CreateRoot($"واحد شرح وظیفه {suffix}", TestNow);
        context.Departments.Add(department);
        await context.SaveChangesAsync(CancellationToken.None);

        var record = JobDescriptionRecord.Create(department.Id, $"پرسنل {suffix}", TestNow);
        var skill = SkillCatalogItem.Create(department.Id, $"مهارت {suffix}", TestNow);
        context.JobDescriptionRecords.Add(record);
        context.SkillCatalogItems.Add(skill);
        await context.SaveChangesAsync(CancellationToken.None);

        var previous = JobDescriptionVersion.Create(
            record.PersonName,
            department.Id,
            $"P-{suffix}",
            "لیسانس",
            "نرم افزار",
            "سه سال",
            [skill.Id],
            [],
            TestNow,
            record.Id);
        var latest = JobDescriptionVersion.Create(
            record.PersonName,
            department.Id,
            $"P-{suffix}",
            "لیسانس",
            "نرم افزار",
            "پنج سال",
            [skill.Id],
            [],
            TestNow.AddMinutes(1),
            record.Id);
        context.JobDescriptionVersions.AddRange(previous, latest);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var result = await new JobDescriptionRepository(context)
            .ListAsync([department.Id], null, CancellationToken.None);

        var personRows = result.Where(item => item.PersonName == record.PersonName).ToArray();
        Assert.Single(personRows);
        Assert.Equal(latest.Id, personRows[0].Id);
    }

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
        Assert.Equal("Updated", persisted.FirstName);
        Assert.Equal("Profile", persisted.LastName);
        Assert.Equal("updated-protected-test-value", persisted.ProtectedMobileNumber);
        Assert.Equal("***1111", persisted.MaskedMobileNumber);
        Assert.Contains(persisted.UserRoles, assignment => assignment.RoleId == role.Id);
    }

    [Fact]
    public async Task AdministrationLookupReader_returns_user_details_for_edit_form()
    {
        await using var context = database.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var department = Department.CreateRoot($"واحد جزئیات {suffix}", TestNow);
        var role = Role.Create($"role-details-{suffix}", "نقش جزئیات", true, TestNow);
        context.Departments.Add(department);
        context.Roles.Add(role);
        await context.SaveChangesAsync(CancellationToken.None);

        var user = User.Create(
            $"org-details-{suffix}",
            "کاربر",
            "جزئیات",
            "protected-details",
            "***2222",
            department.Id,
            TestNow);
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        user.AssignRole(role.Id);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var reader = new AdministrationLookupReader(context);

        var found = await reader.GetUserAsync(user.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
        Assert.Equal(department.Name, found.DepartmentName);
        Assert.Equal(new[] { role.Id }, found.RoleIds);
    }

    [Fact]
    public async Task ManageUsers_update_persists_existing_roles_without_error()
    {
        await using var context = database.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var department = Department.CreateRoot($"واحد ویرایش {suffix}", TestNow);
        context.Departments.Add(department);
        await context.SaveChangesAsync(CancellationToken.None);

        var role = await context.Roles.AsNoTracking()
            .SingleAsync(item => item.Code == SystemRoleCodes.DepartmentManager, CancellationToken.None);
        var user = User.Create(
            $"ORG-UPDATE-{suffix}".ToUpperInvariant(),
            "کاربر",
            "ویرایش",
            "protected-update",
            "***3333",
            department.Id,
            TestNow);
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        user.SetLocalCredentials($"USER-{suffix}", "existing-hash", TestNow);
        user.AssignRole(role.Id);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var found = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id, CancellationToken.None);
        var useCase = new ManageUsers(
            new FixedClock(TestNow.AddMinutes(1)),
            new FixedCorrelationContext($"trace-update-{suffix}"),
            new UserRepository(context),
            new RoleRepository(context),
            new DepartmentRepository(context),
            new UserSessionRepository(context),
            new NoopMobileProtector(),
            new NoopPasswordHasher(),
            new AuditWriter(context, new FixedClock(TestNow.AddMinutes(1)), new FixedCorrelationContext($"trace-update-{suffix}")),
            new EfUnitOfWork(context));

        var result = await useCase.UpdateAsync(
            found.Id,
            new UpdateUserCommand(
                found.Id,
                found.OrganizationalId,
                found.FirstName,
                found.LastName,
                null,
                found.Username!,
                found.DepartmentId,
                [role.Id],
                found.RowVersion),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.Succeeded, result.Status);
        context.ChangeTracker.Clear();
        var assignments = await context.UserRoles.AsNoTracking()
            .Where(item => item.UserId == found.Id)
            .Select(item => item.RoleId)
            .ToArrayAsync(CancellationToken.None);
        Assert.Equal(new[] { role.Id }, assignments);
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
        var preference = UserPreference.Create(user.Id, "system", "navy-teal", false, true, TestNow);
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
    public async Task AuditWriter_PersistsSafeRecordWithLocalTimestamp()
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
        Assert.Equal(TestNow, audit.OccurredAt);
        Assert.Equal("authentication.test", audit.EventCode);
        using var metadata = JsonDocument.Parse(audit.SafeMetadata!);
        Assert.Equal("synthetic", metadata.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task AuditWriter_persists_request_ip_and_coarse_device_kind()
    {
        await using var context = database.CreateDbContext();
        var writer = new AuditWriter(context, new FixedClock(TestNow));
        var unitOfWork = new EfUnitOfWork(context);
        var record = new AuditRecord(
            null,
            null,
            "administration.test",
            true,
            "request-attribution-test",
            null,
            "192.0.2.31",
            "Mobile");

        await writer.WriteAsync(record, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var audit = await context.Set<AuditLog>()
            .AsNoTracking()
            .SingleAsync(item => item.TraceId == "request-attribution-test", CancellationToken.None);
        Assert.Equal("192.0.2.31", audit.ClientIpAddress);
        Assert.Equal("Mobile", audit.ClientDeviceKind);
    }

    private static User CreateUser(string suffix) => User.Create(
        $"org-{suffix}",
        "Test",
        "User",
        "protected-test-value",
        "***0000",
        1,
        TestNow);

    private static OtpChallenge CreateChallenge(long userId, DateTime createdAt, string label) =>
        OtpChallenge.Create(
            userId,
            $"challenge-{label}-{Guid.NewGuid():N}",
            new string('a', 64),
            createdAt,
            createdAt.AddMinutes(5));

    private static async Task<User> AddUserAsync(Microsoft.EntityFrameworkCore.DbContext context)
    {
        var user = CreateUser(Guid.NewGuid().ToString("N"));
        context.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();
        return user;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime Now { get; } = utcNow;
    }

    private sealed class FixedCorrelationContext(string traceId) : ICorrelationContext
    {
        public string TraceId { get; } = traceId;
    }

    private sealed class NoopMobileProtector : IMobileProtector
    {
        public string Protect(string normalizedMobile) => throw new NotSupportedException();

        public string Unprotect(string protectedMobile) => throw new NotSupportedException();

        public string Mask(string normalizedMobile) => throw new NotSupportedException();
    }

    private sealed class NoopPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => throw new NotSupportedException();

        public PasswordVerificationResult Verify(string password, string passwordHash) => throw new NotSupportedException();
    }

}
