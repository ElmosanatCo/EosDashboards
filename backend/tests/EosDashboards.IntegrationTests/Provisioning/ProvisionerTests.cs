using System.Security.Cryptography;
using System.Text;
using EosDashboards.AdminProvisioner;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Provisioning;
using EosDashboards.Domain.Entities;
using EosDashboards.Infrastructure.Persistence;
using EosDashboards.Infrastructure.Persistence.Repositories;
using EosDashboards.Infrastructure.Security;
using EosDashboards.IntegrationTests.Database;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EosDashboards.IntegrationTests.Provisioning;

[Collection(SqlServerDatabaseCollection.Name)]
public sealed class ProvisionerTests(SqlServerDatabaseFixture database)
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 9, 2, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RepeatedProvisioningPersistsOneAdministratorAndTwoSafeAudits()
    {
        using var keyRing = TemporaryKeyRing.Create();
        var mobileProtector = new DataProtectionMobileProtector(
            DataProtectionProvider.Create(keyRing.Path));
        var firstCommand = new ProvisionSystemAdministratorCommand(
            "  org-synthetic-integration  ",
            "  domain\\synthetic.one  ",
            "local.integration.one",
            "first synthetic password",
            "SyntheticFirstOne",
            "SyntheticLastOne",
            "09120006789");
        var secondCommand = new ProvisionSystemAdministratorCommand(
            "ORG-SYNTHETIC-INTEGRATION",
            "DOMAIN\\SYNTHETIC.TWO",
            "local.integration.two",
            "second synthetic password",
            "SyntheticFirstTwo",
            "SyntheticLastTwo",
            "09350006789");

        var firstResult = await ProvisionOnceAsync(firstCommand, mobileProtector);
        var secondResult = await ProvisionOnceAsync(secondCommand, mobileProtector);

        await using var verificationContext = database.CreateDbContext();
        var persistedUser = await new UserRepository(verificationContext)
            .FindByOrganizationalIdAsync(
                "ORG-SYNTHETIC-INTEGRATION",
                CancellationToken.None);
        var persistedRole = await new RoleRepository(verificationContext)
            .FindByCodeAsync("SystemAdministrator", CancellationToken.None);
        Assert.NotNull(persistedUser);
        Assert.NotNull(persistedRole);
        Assert.Equal(
            1,
            await verificationContext.Users.CountAsync(
                item => item.OrganizationalId == "ORG-SYNTHETIC-INTEGRATION"));
        Assert.Equal(
            1,
            await verificationContext.Roles.CountAsync(
                item => item.Code == "SystemAdministrator"));
        Assert.True(persistedUser!.IsActive);
        Assert.Equal("DOMAIN\\SYNTHETIC.TWO", persistedUser.AccountName);
        Assert.Equal("SyntheticFirstTwo", persistedUser.FirstName);
        Assert.Equal("SyntheticLastTwo", persistedUser.LastName);
        Assert.NotEqual("09350006789", persistedUser.ProtectedMobileNumber);
        Assert.Equal(
            "09350006789",
            mobileProtector.Unprotect(persistedUser.ProtectedMobileNumber));
        Assert.Equal("*******6789", persistedUser.MaskedMobileNumber);
        Assert.Equal("SystemAdministrator", persistedRole!.Code);
        Assert.Equal("مدیر سامانه", persistedRole.DisplayName);
        Assert.True(persistedRole.IsSystem);
        Assert.True(persistedRole.IsActive);
        var departmentManagerRole = await verificationContext.Roles
            .AsNoTracking()
            .SingleAsync(item => item.Code == "DepartmentManager");
        Assert.Equal(2, persistedUser.UserRoles.Count);
        Assert.Contains(persistedUser.UserRoles, assignment => assignment.RoleId == persistedRole.Id);
        Assert.Contains(persistedUser.UserRoles, assignment => assignment.RoleId == departmentManagerRole.Id);
        Assert.Equal(1, persistedUser.DepartmentId);
        Assert.Equal(persistedUser.Id, firstResult.UserId);
        Assert.Equal(persistedUser.Id, secondResult.UserId);
        Assert.Equal("*******6789", firstResult.MaskedMobile);
        Assert.Equal("*******6789", secondResult.MaskedMobile);

        var audits = await verificationContext.AuditLogs
            .AsNoTracking()
            .Where(item =>
                item.EventCode == "SystemAdministratorProvisioned" &&
                item.SubjectUserId == persistedUser.Id)
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.All(audits, audit =>
        {
            Assert.Null(audit.ActorUserId);
            Assert.Equal(persistedUser.Id, audit.SubjectUserId);
            Assert.True(audit.Succeeded);
            Assert.Equal("trace-synthetic-provisioning", audit.TraceId);
            Assert.Null(audit.SafeMetadata);
        });

        var auditText = string.Join(Environment.NewLine, audits.Select(item => item.ToString()));
        Assert.DoesNotContain("09120006789", auditText, StringComparison.Ordinal);
        Assert.DoesNotContain("09350006789", auditText, StringComparison.Ordinal);
        Assert.DoesNotContain("SyntheticFirst", auditText, StringComparison.Ordinal);
        Assert.DoesNotContain("SyntheticLast", auditText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProvisioningPersistsOnePendingGoogleLinkWithoutAuditingTheEmail()
    {
        await ResetProvisioningStateAsync();
        try
        {
            using var keyRing = TemporaryKeyRing.Create();
            var mobileProtector = new DataProtectionMobileProtector(
                DataProtectionProvider.Create(keyRing.Path));
            await ProvisionOnceAsync(
                new ProvisionSystemAdministratorCommand(
                    "org-synthetic-google-integration",
                    "domain\\synthetic.google.integration",
                    "local.google.integration",
                    "synthetic password",
                    "Synthetic",
                    "Google",
                    "09120006789",
                    "person.synthetic@example.test"),
                mobileProtector);

            await using var verificationContext = database.CreateDbContext();
            var link = Assert.Single(await verificationContext.ExternalIdentityLinks
                .AsNoTracking()
                .Where(item => item.NormalizedEmail == "PERSON.SYNTHETIC@EXAMPLE.TEST")
                .ToListAsync());
            Assert.Equal(EosDashboards.Domain.Enums.ExternalIdentityProvider.Google, link.Provider);
            Assert.Null(link.ProviderSubject);
            Assert.Null(link.LinkedAtUtc);

            var auditText = string.Join(
                Environment.NewLine,
                await verificationContext.AuditLogs
                    .AsNoTracking()
                    .Where(item => item.SubjectUserId == link.UserId)
                    .Select(item => item.SafeMetadata)
                    .ToListAsync());
            Assert.DoesNotContain("person.synthetic@example.test", auditText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await ResetProvisioningStateAsync();
        }
    }

    [Fact]
    public async Task AuditFailureRollsBackGeneratedUserAndAnyStagedAudit()
    {
        using var keyRing = TemporaryKeyRing.Create();
        var mobileProtector = new DataProtectionMobileProtector(
            DataProtectionProvider.Create(keyRing.Path));
        await using var context = database.CreateDbContext();
        var auditWriter = new ThrowAfterStagingAuditWriter(
            new AuditWriter(context, new FixedClock(TestNow)));
        var sut = new ProvisionSystemAdministrator(
            new FixedClock(TestNow),
            new FixedCorrelationContext("trace-synthetic-rollback"),
                new UserRepository(context),
                new RoleRepository(context),
                new DepartmentRepository(context),
                mobileProtector,
                new LocalPasswordHasher(),
                auditWriter,
                new ExternalIdentityLinkRepository(context),
            new EfUnitOfWork(context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.HandleAsync(
                new ProvisionSystemAdministratorCommand(
                    "ORG-SYNTHETIC-ROLLBACK",
                    "DOMAIN\\SYNTHETIC.ROLLBACK",
                    "local.rollback",
                    "synthetic password",
                    "Synthetic",
                    "Rollback",
                    "09120006789"),
                CancellationToken.None));

        await using var verificationContext = database.CreateDbContext();
        Assert.False(await verificationContext.Users.AnyAsync(
            item => item.OrganizationalId == "ORG-SYNTHETIC-ROLLBACK"));
        Assert.False(await verificationContext.AuditLogs.AnyAsync(
            item => item.TraceId == "trace-synthetic-rollback"));
    }

    [Fact]
    public async Task CaseInsensitiveRoleLookupRejectsMisCasedStoredCodeWithoutDisclosure()
    {
        const string storedRoleCode = "systemadministrator";
        const string organizationalId = "ORG-SYNTHETIC-ROLE-DRIFT";
        const string accountName = "DOMAIN\\SYNTHETIC.ROLE-DRIFT";
        const string firstName = "SyntheticRoleDriftFirst";
        const string lastName = "SyntheticRoleDriftLast";
        const string mobile = "09120003333";
        await ResetProvisioningStateAsync();

        try
        {
            long roleId;
            await using (var seedContext = database.CreateDbContext())
            {
                var role = Role.Create(storedRoleCode, "مدیر سامانه", true, TestNow);
                seedContext.Roles.Add(role);
                await seedContext.SaveChangesAsync();
                roleId = role.Id;
            }

            using var keyRing = TemporaryKeyRing.Create();
            var mobileProtector = new DataProtectionMobileProtector(
                DataProtectionProvider.Create(keyRing.Path));
            await using var context = database.CreateDbContext();
            var sut = new ProvisionSystemAdministrator(
                new FixedClock(TestNow),
                new FixedCorrelationContext("trace-synthetic-role-drift"),
                new UserRepository(context),
                new RoleRepository(context),
                new DepartmentRepository(context),
                mobileProtector,
                new LocalPasswordHasher(),
                new AuditWriter(context, new FixedClock(TestNow)),
                new ExternalIdentityLinkRepository(context),
                new EfUnitOfWork(context));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.HandleAsync(
                    new ProvisionSystemAdministratorCommand(
                        organizationalId,
                        accountName,
                        "local.role-drift",
                        "synthetic password",
                        firstName,
                        lastName,
                        mobile),
                    CancellationToken.None));

            Assert.Equal("A fixed system role is not valid.", exception.Message);
            var diagnosticText = exception.Message;
            Assert.DoesNotContain(storedRoleCode, diagnosticText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(organizationalId, diagnosticText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(accountName, diagnosticText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(firstName, diagnosticText, StringComparison.Ordinal);
            Assert.DoesNotContain(lastName, diagnosticText, StringComparison.Ordinal);
            Assert.DoesNotContain(mobile, diagnosticText, StringComparison.Ordinal);

            await using var verificationContext = database.CreateDbContext();
            var roleAfterFailure = await verificationContext.Roles
                .AsNoTracking()
                .SingleAsync(item => item.Id == roleId);
            Assert.Equal(storedRoleCode, roleAfterFailure.Code);
            Assert.False(await verificationContext.Users.AnyAsync(
                item => item.OrganizationalId == organizationalId));
            Assert.False(await verificationContext.UserRoles.AnyAsync(
                item => item.RoleId == roleId));
            Assert.False(await verificationContext.AuditLogs.AnyAsync(
                item => item.TraceId == "trace-synthetic-role-drift"));
        }
        finally
        {
            await ResetProvisioningStateAsync();
        }
    }

    [Fact]
    public async Task ConcurrentProvisioningSerializesAcrossContextsWithoutTornProfileData()
    {
        await ResetProvisioningStateAsync();
        using var keyRing = TemporaryKeyRing.Create();
        var mobileProtector = new DataProtectionMobileProtector(
            DataProtectionProvider.Create(keyRing.Path));
        var coordinator = new TwoParticipantReadCoordinator();
        var firstCommand = new ProvisionSystemAdministratorCommand(
            "org-synthetic-concurrent",
            "domain\\synthetic.concurrent-one",
            "local.concurrent.one",
            "first synthetic password",
            "SyntheticConcurrentFirstOne",
            "SyntheticConcurrentLastOne",
            "09120001111");
        var secondCommand = new ProvisionSystemAdministratorCommand(
            "ORG-SYNTHETIC-CONCURRENT",
            "DOMAIN\\SYNTHETIC.CONCURRENT-TWO",
            "local.concurrent.two",
            "second synthetic password",
            "SyntheticConcurrentFirstTwo",
            "SyntheticConcurrentLastTwo",
            "09350002222");

        try
        {
            var results = await Task.WhenAll(
                ProvisionConcurrentlyOnceAsync(firstCommand, mobileProtector, coordinator),
                ProvisionConcurrentlyOnceAsync(secondCommand, mobileProtector, coordinator));

            Assert.Equal(2, results.Length);
            Assert.Equal(
                ["*******1111", "*******2222"],
                results.Select(item => item.MaskedMobile).Order().ToArray());
            await using var verificationContext = database.CreateDbContext();
            var user = await verificationContext.Users
                .AsNoTracking()
                .Include(item => item.UserRoles)
                .SingleAsync(item => item.OrganizationalId == "ORG-SYNTHETIC-CONCURRENT");
            var role = await verificationContext.Roles
                .AsNoTracking()
                .SingleAsync(item => item.Code == "SystemAdministrator");
            Assert.True(user.IsActive);
            Assert.Equal(
                1,
                await verificationContext.Users.CountAsync(
                    item => item.OrganizationalId == "ORG-SYNTHETIC-CONCURRENT"));
            Assert.Equal(
                1,
                await verificationContext.Roles.CountAsync(
                    item => item.Code == "SystemAdministrator"));
            var departmentManagerRole = await verificationContext.Roles
                .AsNoTracking()
                .SingleAsync(item => item.Code == "DepartmentManager");
            Assert.Equal(2, user.UserRoles.Count);
            Assert.Contains(user.UserRoles, assignment => assignment.RoleId == role.Id);
            Assert.Contains(user.UserRoles, assignment => assignment.RoleId == departmentManagerRole.Id);
            Assert.Equal(1, user.DepartmentId);

            var completeProfile = (
                user.AccountName,
                user.FirstName,
                user.LastName,
                Mobile: mobileProtector.Unprotect(user.ProtectedMobileNumber),
                user.MaskedMobileNumber);
            var firstExpectedProfile = (
                AccountName: "DOMAIN\\SYNTHETIC.CONCURRENT-ONE",
                FirstName: "SyntheticConcurrentFirstOne",
                LastName: "SyntheticConcurrentLastOne",
                Mobile: "09120001111",
                MaskedMobileNumber: "*******1111");
            var secondExpectedProfile = (
                AccountName: "DOMAIN\\SYNTHETIC.CONCURRENT-TWO",
                FirstName: "SyntheticConcurrentFirstTwo",
                LastName: "SyntheticConcurrentLastTwo",
                Mobile: "09350002222",
                MaskedMobileNumber: "*******2222");
            Assert.True(
                completeProfile == firstExpectedProfile || completeProfile == secondExpectedProfile,
                "The persisted profile must be one complete serialized input.");

            var audits = await verificationContext.AuditLogs
                .AsNoTracking()
                .Where(item =>
                    item.EventCode == "SystemAdministratorProvisioned" &&
                    item.SubjectUserId == user.Id)
                .ToListAsync();
            Assert.Equal(2, audits.Count);
            Assert.All(audits, audit =>
            {
                Assert.Null(audit.ActorUserId);
                Assert.Equal(user.Id, audit.SubjectUserId);
                Assert.True(audit.Succeeded);
                Assert.Null(audit.SafeMetadata);
            });
        }
        finally
        {
            await ResetProvisioningStateAsync();
        }
    }

    [Fact]
    public void ProvisionerCompositionStartsWithoutSmsConfiguration()
    {
        using var keyRing = TemporaryKeyRing.Create();
        var configuration = CreateSyntheticConfiguration(keyRing.Path);
        var services = new ServiceCollection();

        services.AddAdminProvisioner(configuration);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProvisionSystemAdministrator>());
        Assert.Equal(
            "*******6789",
            scope.ServiceProvider.GetRequiredService<IMobileProtector>().Mask("09120006789"));
        Assert.Null(scope.ServiceProvider.GetService<ISmsSender>());
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("Y")]
    [InlineData("بله")]
    public void InteractiveInputUsesHiddenMobileEntryAndShowsOnlyMaskedConfirmation(
        string confirmation)
    {
        var console = new RecordingConsole(
            [
                "org-synthetic-console",
                "domain\\synthetic.console",
                "local.console",
                "Synthetic",
                "Console",
                confirmation,
            ],
            "synthetic password",
            "09120006789",
            "");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.NotNull(command);
        Assert.Equal("09120006789", command!.Mobile);
        Assert.Equal(string.Empty, command.GoogleEmail);
        Assert.Equal(3, console.SecretReadCount);
        Assert.Contains("*******6789", console.Transcript, StringComparison.Ordinal);
        Assert.DoesNotContain("09120006789", console.Transcript, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveInputReadsAnOptionalGoogleEmailWithoutEchoingIt()
    {
        var console = new RecordingConsole(
            [
                "org-synthetic-google-console",
                "domain\\synthetic.google.console",
                "local.google.console",
                "Synthetic",
                "Google",
                "yes",
            ],
            "synthetic password",
            "09120006789",
            "person.synthetic@example.test");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.NotNull(command);
        Assert.Equal("person.synthetic@example.test", command!.GoogleEmail);
        Assert.Equal(3, console.SecretReadCount);
        Assert.DoesNotContain("person.synthetic@example.test", console.Transcript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InteractiveInputPreservesUnicodeProfileNames()
    {
        const string firstName = "\u0646\u0627\u0645";
        const string lastName = "\u0622\u0632\u0645\u0648\u0646";
        var console = new RecordingConsole(
            [
                "org-synthetic-unicode",
                "domain\\synthetic.unicode",
                "local.unicode",
                firstName,
                lastName,
                "yes",
            ],
            "synthetic password",
            "09120006789",
            "");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.NotNull(command);
        Assert.Equal(firstName, command!.FirstName);
        Assert.Equal(lastName, command.LastName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no")]
    [InlineData("حتما")]
    public void InteractiveInputCancelsUnlessConfirmationIsExplicit(string? confirmation)
    {
        var console = new RecordingConsole(
            [
                "org-synthetic-console-cancel",
                "domain\\synthetic.cancel",
                "local.cancel",
                "Synthetic",
                "Cancel",
                confirmation,
            ],
            "synthetic password",
            "09120006789",
            "");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.Null(command);
        Assert.Contains("لغو", console.Transcript, StringComparison.Ordinal);
        Assert.DoesNotContain("09120006789", console.Transcript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunnerRejectsCommandLineValuesWithoutEchoingThem()
    {
        var console = new RecordingConsole([], [null]);

        var exitCode = await AdminProvisionerRunner.RunAsync(
            ["synthetic-private-command-line-value"],
            console,
            new ConfigurationBuilder().Build(),
            CancellationToken.None);

        Assert.Equal(2, exitCode);
        Assert.DoesNotContain(
            "synthetic-private-command-line-value",
            console.Transcript,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunnerReportsOnlyFailureTypeWhenLocalDiagnosticsAreExplicitlyEnabled()
    {
        var console = new RecordingConsole([], [null]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProvisioningDiagnostics:ExposeFailureType"] = "true",
            })
            .Build();

        var exitCode = await AdminProvisionerRunner.RunAsync(
            [],
            console,
            configuration,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("Diagnostic failure type: InvalidOperationException.", console.Transcript, StringComparison.Ordinal);
        Assert.DoesNotContain("A database connection is required", console.Transcript, StringComparison.Ordinal);
    }

    private async Task<ProvisionSystemAdministratorResult> ProvisionOnceAsync(
        ProvisionSystemAdministratorCommand command,
        IMobileProtector mobileProtector)
    {
        await using var context = database.CreateDbContext();
        var sut = new ProvisionSystemAdministrator(
            new FixedClock(TestNow),
            new FixedCorrelationContext("trace-synthetic-provisioning"),
            new UserRepository(context),
            new RoleRepository(context),
            new DepartmentRepository(context),
            mobileProtector,
            new LocalPasswordHasher(),
            new AuditWriter(context, new FixedClock(TestNow)),
            new ExternalIdentityLinkRepository(context),
            new EfUnitOfWork(context));
        return await sut.HandleAsync(command, CancellationToken.None);
    }

    private async Task<ProvisionSystemAdministratorResult> ProvisionConcurrentlyOnceAsync(
        ProvisionSystemAdministratorCommand command,
        IMobileProtector mobileProtector,
        TwoParticipantReadCoordinator coordinator)
    {
        await using var context = database.CreateDbContext();
        var sut = new ProvisionSystemAdministrator(
            new FixedClock(TestNow),
            new FixedCorrelationContext(Guid.NewGuid().ToString("N")),
            new UserRepository(context),
            new CoordinatedRoleRepository(new RoleRepository(context), coordinator),
            new DepartmentRepository(context),
            mobileProtector,
            new LocalPasswordHasher(),
            new AuditWriter(context, new FixedClock(TestNow)),
            new ExternalIdentityLinkRepository(context),
            new EfUnitOfWork(context));
        return await sut.HandleAsync(command, CancellationToken.None);
    }

    private async Task ResetProvisioningStateAsync()
    {
        await using var context = database.CreateDbContext();
        await context.AuditLogs
            .Where(item => item.EventCode == "SystemAdministratorProvisioned")
            .ExecuteDeleteAsync();
        var syntheticUserIds = await context.Users
            .Where(item => item.OrganizationalId.StartsWith("ORG-SYNTHETIC-"))
            .Select(item => item.Id)
            .ToArrayAsync();
        if (syntheticUserIds.Length > 0)
        {
            await context.ExternalIdentityLinks
                .Where(item => syntheticUserIds.Contains(item.UserId))
                .ExecuteDeleteAsync();
        }
        var roleIds = await context.Roles
            .Where(item => item.Code.ToUpper() == "SYSTEMADMINISTRATOR")
            .Select(item => item.Id)
            .ToArrayAsync();
        if (roleIds.Length > 0)
        {
            await context.UserRoles
                .Where(item => roleIds.Contains(item.RoleId))
                .ExecuteDeleteAsync();
        }

        await context.Users
            .Where(item => item.OrganizationalId.StartsWith("ORG-SYNTHETIC-"))
            .ExecuteDeleteAsync();
        await context.Roles
            .Where(item => item.Code.ToUpper() == "SYSTEMADMINISTRATOR")
            .ExecuteDeleteAsync();
    }

    private static IConfiguration CreateSyntheticConfiguration(string keyRingPath)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = "EosDashboards_Codex_IntegrationTests",
            IntegratedSecurity = true,
        }.ConnectionString;
        var hashingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EosDashboard"] = connectionString,
                ["AuthSecurity:HashingKey"] = hashingKey,
                ["AuthSecurity:SigningKey"] = signingKey,
                ["AuthSecurity:Issuer"] = "synthetic-issuer",
                ["AuthSecurity:Audience"] = "synthetic-audience",
                ["AuthSecurity:AccessTokenLifetime"] = "00:10:00",
                ["AuthSecurity:SessionLifetime"] = "08:00:00",
                ["AuthSecurity:KeyRingPath"] = keyRingPath,
            })
            .Build();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedCorrelationContext(string traceId) : ICorrelationContext
    {
        public string TraceId { get; } = traceId;
    }

    private sealed class ThrowAfterStagingAuditWriter(IAuditWriter inner) : IAuditWriter
    {
        public async Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
        {
            await inner.WriteAsync(record, cancellationToken);
            throw new InvalidOperationException("Synthetic audit persistence failure.");
        }
    }

    private sealed class MaskOnlyMobileProtector : IMobileProtector
    {
        public string Protect(string normalizedMobile) => throw new NotSupportedException();

        public string Unprotect(string protectedMobile) => throw new NotSupportedException();

        public string Mask(string normalizedMobile) => $"*******{normalizedMobile[^4..]}";
    }

    private sealed class CoordinatedRoleRepository(
        IRoleRepository inner,
        TwoParticipantReadCoordinator coordinator) : IRoleRepository
    {
        public async Task<Role?> FindByCodeAsync(
            string code,
            CancellationToken cancellationToken)
        {
            var role = await inner.FindByCodeAsync(code, cancellationToken);
            await coordinator.SynchronizeAsync(cancellationToken);
            return role;
        }

        public void Add(Role role) => inner.Add(role);

        public Task<IReadOnlyList<Role>> GetByIdsAsync(
            IReadOnlyCollection<long> ids,
            CancellationToken cancellationToken) =>
            inner.GetByIdsAsync(ids, cancellationToken);
    }

    private sealed class TwoParticipantReadCoordinator
    {
        private readonly TaskCompletionSource _bothArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivalCount;

        public async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrivalCount) == 2)
            {
                _bothArrived.TrySetResult();
                return;
            }

            try
            {
                await _bothArrived.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (TimeoutException)
            {
                // A database-serialized second caller cannot reach this read until the first commits.
            }
        }
    }

    private sealed class RecordingConsole(
        IEnumerable<string?> lineInputs,
        params string?[] secretInputs) : IInteractiveConsole
    {
        private readonly Queue<string?> _lineInputs = new(lineInputs);
        private readonly Queue<string?> _secretInputs = new(secretInputs);
        private readonly StringBuilder _transcript = new();

        public int SecretReadCount { get; private set; }

        public string Transcript => _transcript.ToString();

        public void Write(string value) => _transcript.Append(value);

        public void WriteLine(string value) => _transcript.AppendLine(value);

        public string? ReadLine() => _lineInputs.Count == 0 ? null : _lineInputs.Dequeue();

        public string? ReadSecret()
        {
            SecretReadCount++;
            return _secretInputs.Count == 0 ? null : _secretInputs.Dequeue();
        }
    }

    private sealed class TemporaryKeyRing : IDisposable
    {
        private static readonly string RootPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "EosDashboards",
                "ProvisioningTests"));

        private TemporaryKeyRing(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryKeyRing Create()
        {
            var path = System.IO.Path.Combine(RootPath, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryKeyRing(path);
        }

        public void Dispose()
        {
            var resolvedPath = System.IO.Path.GetFullPath(Path);
            var parent = Directory.GetParent(resolvedPath)?.FullName;
            if (!string.Equals(parent, RootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unexpected test key-ring cleanup path.");
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
            }
        }
    }
}
