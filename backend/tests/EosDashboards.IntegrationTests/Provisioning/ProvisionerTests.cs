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
            "SyntheticFirstOne",
            "SyntheticLastOne",
            "09120006789");
        var secondCommand = new ProvisionSystemAdministratorCommand(
            "ORG-SYNTHETIC-INTEGRATION",
            "DOMAIN\\SYNTHETIC.TWO",
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
        var assignment = Assert.Single(persistedUser.UserRoles);
        Assert.Equal(persistedUser.Id, assignment.UserId);
        Assert.Equal(persistedRole.Id, assignment.RoleId);
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
            mobileProtector,
            auditWriter,
            new EfUnitOfWork(context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.HandleAsync(
                new ProvisionSystemAdministratorCommand(
                    "ORG-SYNTHETIC-ROLLBACK",
                    "DOMAIN\\SYNTHETIC.ROLLBACK",
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
                "Synthetic",
                "Console",
                confirmation,
            ],
            "09120006789");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.NotNull(command);
        Assert.Equal("09120006789", command!.Mobile);
        Assert.Equal(1, console.SecretReadCount);
        Assert.Contains("*******6789", console.Transcript, StringComparison.Ordinal);
        Assert.DoesNotContain("09120006789", console.Transcript, StringComparison.Ordinal);
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
                "Synthetic",
                "Cancel",
                confirmation,
            ],
            "09120006789");
        var input = new InteractiveInput(console, new MaskOnlyMobileProtector());

        var command = input.Read();

        Assert.Null(command);
        Assert.Contains("لغو", console.Transcript, StringComparison.Ordinal);
        Assert.DoesNotContain("09120006789", console.Transcript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunnerRejectsCommandLineValuesWithoutEchoingThem()
    {
        var console = new RecordingConsole([], null);

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
            mobileProtector,
            new AuditWriter(context, new FixedClock(TestNow)),
            new EfUnitOfWork(context));
        return await sut.HandleAsync(command, CancellationToken.None);
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

    private sealed class RecordingConsole(
        IEnumerable<string?> lineInputs,
        string? secretInput) : IInteractiveConsole
    {
        private readonly Queue<string?> _lineInputs = new(lineInputs);
        private readonly StringBuilder _transcript = new();

        public int SecretReadCount { get; private set; }

        public string Transcript => _transcript.ToString();

        public void Write(string value) => _transcript.Append(value);

        public void WriteLine(string value) => _transcript.AppendLine(value);

        public string? ReadLine() => _lineInputs.Count == 0 ? null : _lineInputs.Dequeue();

        public string? ReadSecret()
        {
            SecretReadCount++;
            return secretInput;
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
