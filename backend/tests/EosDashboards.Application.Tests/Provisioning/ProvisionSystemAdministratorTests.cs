using System.Reflection;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Provisioning;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.Provisioning;

public sealed class ProvisionSystemAdministratorTests
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RepeatedProvisioningKeepsOneActiveAdministratorAndAuditsEachOperation()
    {
        var dependencies = new ProvisioningDependencies();
        var sut = dependencies.CreateSut();
        var firstCommand = new ProvisionSystemAdministratorCommand(
            "  org-synthetic-7  ",
            "  domain\\synthetic.one  ",
            "  local.admin.one  ",
            "first synthetic password",
            "  SyntheticFirstOne  ",
            "  SyntheticLastOne  ",
            "09120006789");
        var secondCommand = new ProvisionSystemAdministratorCommand(
            "ORG-SYNTHETIC-7",
            "DOMAIN\\SYNTHETIC.TWO",
            "LOCAL.ADMIN.TWO",
            "second synthetic password",
            "  SyntheticFirstTwo  ",
            "  SyntheticLastTwo  ",
            "09350006789");

        var firstResult = await sut.HandleAsync(firstCommand, CancellationToken.None);
        var secondResult = await sut.HandleAsync(secondCommand, CancellationToken.None);

        var user = Assert.Single(dependencies.Users.Items);
        var role = Assert.Single(dependencies.Roles.Items);
        Assert.True(user.IsActive);
        Assert.Equal("ORG-SYNTHETIC-7", user.OrganizationalId);
        Assert.Equal("DOMAIN\\SYNTHETIC.TWO", user.AccountName);
        Assert.Equal("LOCAL.ADMIN.TWO", user.Username);
        Assert.Equal("hash:second synthetic password", user.PasswordHash);
        Assert.Equal("SyntheticFirstTwo", user.FirstName);
        Assert.Equal("SyntheticLastTwo", user.LastName);
        Assert.Equal("protected-mobile-two", user.ProtectedMobileNumber);
        Assert.Equal("*******6789", user.MaskedMobileNumber);
        Assert.Equal("SystemAdministrator", role.Code);
        Assert.Equal("مدیر سامانه", role.DisplayName);
        Assert.True(role.IsSystem);
        Assert.True(role.IsActive);
        var assignment = Assert.Single(user.UserRoles);
        Assert.Equal(user.Id, assignment.UserId);
        Assert.Equal(role.Id, assignment.RoleId);
        Assert.True(user.Id > 0);
        Assert.True(role.Id > 0);

        Assert.Equal(user.Id, firstResult.UserId);
        Assert.Equal(user.Id, secondResult.UserId);
        Assert.Equal("*******6789", firstResult.MaskedMobile);
        Assert.Equal("*******6789", secondResult.MaskedMobile);
        Assert.Equal(2, dependencies.UnitOfWork.TransactionCount);
        Assert.Equal([2, 1], dependencies.UnitOfWork.SaveCountsByTransaction);

        Assert.Collection(
            dependencies.Audits.Records,
            record => AssertProvisioningAudit(record, user.Id),
            record => AssertProvisioningAudit(record, user.Id));

        var sensitiveValues = new[]
        {
            "org-synthetic-7",
            "domain\\synthetic.one",
            "domain\\synthetic.two",
            "local.admin.one",
            "local.admin.two",
            "first synthetic password",
            "second synthetic password",
            "SyntheticFirstOne",
            "SyntheticLastOne",
            "09120006789",
            "SyntheticFirstTwo",
            "SyntheticLastTwo",
            "09350006789",
        };
        var safeText = string.Join(
            Environment.NewLine,
            firstCommand,
            secondCommand,
            firstResult,
            secondResult,
            string.Join(Environment.NewLine, dependencies.Audits.Records));
        Assert.All(
            sensitiveValues,
            value => Assert.DoesNotContain(value, safeText, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProvisioningReactivatesAnExistingUser()
    {
        var dependencies = new ProvisioningDependencies();
        var role = Role.Create("SystemAdministrator", "مدیر سامانه", true, TestNow);
        SetId(role, 31);
        dependencies.Roles.Items.Add(role);
        var user = User.Create(
            "ORG-SYNTHETIC-INACTIVE",
            "DOMAIN\\SYNTHETIC.INACTIVE",
            "Previous",
            "Profile",
            "previous-protected-mobile",
            "*******0000",
            TestNow.AddDays(-1));
        SetId(user, 41);
        user.Deactivate(TestNow.AddHours(-1));
        dependencies.Users.Items.Add(user);
        var sut = dependencies.CreateSut();

        await sut.HandleAsync(
            new ProvisionSystemAdministratorCommand(
                "org-synthetic-inactive",
                "domain\\synthetic.active",
                "local.admin.active",
                "synthetic password",
                "Synthetic",
                "Active",
                "09120006789"),
            CancellationToken.None);

        Assert.True(user.IsActive);
        Assert.Null(user.DeactivatedAtUtc);
        Assert.Single(user.UserRoles);
        Assert.Equal([1], dependencies.UnitOfWork.SaveCountsByTransaction);
    }

    [Theory]
    [InlineData("")]
    [InlineData("9120006789")]
    [InlineData("08120006789")]
    [InlineData("0912000678")]
    [InlineData("091200067890")]
    [InlineData("0912-invalid")]
    [InlineData("۰۹۱۲۰۰۰۶۷۸۹")]
    public async Task InvalidMobileIsRejectedWithoutDisclosure(string invalidMobile)
    {
        var dependencies = new ProvisioningDependencies();
        var sut = dependencies.CreateSut();
        var command = new ProvisionSystemAdministratorCommand(
            "org-synthetic-invalid",
            "domain\\synthetic.invalid",
            "local.admin.invalid",
            "synthetic password",
            "Synthetic",
            "Invalid",
            invalidMobile);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.HandleAsync(command, CancellationToken.None));

        if (invalidMobile.Length > 0)
        {
            Assert.DoesNotContain(invalidMobile, exception.ToString(), StringComparison.Ordinal);
        }

        Assert.Empty(dependencies.Users.Items);
        Assert.Empty(dependencies.Roles.Items);
        Assert.Empty(dependencies.Audits.Records);
        Assert.Equal(0, dependencies.UnitOfWork.TransactionCount);
    }

    [Theory]
    [InlineData("", "DOMAIN\\SYNTHETIC", "Synthetic", "Person")]
    [InlineData("ORG-SYNTHETIC", "", "Synthetic", "Person")]
    [InlineData("ORG-SYNTHETIC", "DOMAIN\\SYNTHETIC", "", "Person")]
    [InlineData("ORG-SYNTHETIC", "DOMAIN\\SYNTHETIC", "Synthetic", "")]
    public async Task BlankIdentityOrNameIsRejectedBeforePersistence(
        string organizationalId,
        string accountName,
        string firstName,
        string lastName)
    {
        var dependencies = new ProvisioningDependencies();
        var sut = dependencies.CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.HandleAsync(
                new ProvisionSystemAdministratorCommand(
                    organizationalId,
                    accountName,
                    "local.admin",
                    "synthetic password",
                    firstName,
                    lastName,
                    "09120006789"),
                CancellationToken.None));

        Assert.Empty(dependencies.Users.Items);
        Assert.Empty(dependencies.Roles.Items);
        Assert.Empty(dependencies.Audits.Records);
        Assert.Equal(0, dependencies.UnitOfWork.TransactionCount);
    }

    [Fact]
    public async Task ExistingInvalidSystemRoleIsRejectedWithoutChangingUser()
    {
        var dependencies = new ProvisioningDependencies();
        var invalidRole = Role.Create("SystemAdministrator", "Unexpected", false, TestNow);
        SetId(invalidRole, 51);
        dependencies.Roles.Items.Add(invalidRole);
        var sut = dependencies.CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.HandleAsync(
                new ProvisionSystemAdministratorCommand(
                    "org-synthetic-role-check",
                    "domain\\synthetic.rolecheck",
                    "local.admin.rolecheck",
                    "synthetic password",
                    "Synthetic",
                    "RoleCheck",
                    "09120006789"),
                CancellationToken.None));

        Assert.Equal("The system administrator role is not valid.", exception.Message);
        Assert.Empty(dependencies.Users.Items);
        Assert.Empty(dependencies.Audits.Records);
    }

    private static void AssertProvisioningAudit(AuditRecord record, long expectedSubjectUserId)
    {
        Assert.Null(record.ActorUserId);
        Assert.Equal(expectedSubjectUserId, record.SubjectUserId);
        Assert.Equal("SystemAdministratorProvisioned", record.EventCode);
        Assert.True(record.Succeeded);
        Assert.Equal("trace-provisioning-test", record.TraceId);
        Assert.Null(record.SafeMetadata);
    }

    private static void SetId<T>(T entity, long id)
    {
        typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, id);
    }

    private sealed class ProvisioningDependencies
    {
        public TestUserRepository Users { get; } = new();

        public TestRoleRepository Roles { get; } = new();

        public TestMobileProtector MobileProtector { get; } = new();

        public TestPasswordHasher PasswordHasher { get; } = new();

        public TestAuditWriter Audits { get; } = new();

        public TestUnitOfWork UnitOfWork { get; }

        public ProvisioningDependencies()
        {
            UnitOfWork = new TestUnitOfWork(Users, Roles);
        }

        public ProvisionSystemAdministrator CreateSut() => new(
            new TestClock(TestNow),
            new TestCorrelationContext(),
            Users,
            Roles,
            MobileProtector,
            PasswordHasher,
            Audits,
            UnitOfWork);
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestCorrelationContext : ICorrelationContext
    {
        public string TraceId => "trace-provisioning-test";
    }

    private sealed class TestUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> FindByOrganizationalIdAsync(
            string stableId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.OrganizationalId == stableId));
        }

        public Task<User?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Username == username));
        }

        public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public void Add(User user) => Items.Add(user);
    }

    private sealed class TestRoleRepository : IRoleRepository
    {
        public List<Role> Items { get; } = [];

        public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Code == code));
        }

        public void Add(Role role) => Items.Add(role);
    }

    private sealed class TestMobileProtector : IMobileProtector
    {
        private int _protectCount;

        public string Protect(string normalizedMobile)
        {
            _protectCount++;
            return $"protected-mobile-{(_protectCount == 1 ? "one" : "two")}";
        }

        public string Unprotect(string protectedMobile) => throw new NotSupportedException();

        public string Mask(string normalizedMobile) => $"*******{normalizedMobile[^4..]}";
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public PasswordVerificationResult Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class TestAuditWriter : IAuditWriter
    {
        public List<AuditRecord> Records { get; } = [];

        public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class TestUnitOfWork(
        TestUserRepository users,
        TestRoleRepository roles) : IUnitOfWork
    {
        private long _nextUserId = 101;
        private long _nextRoleId = 201;

        public int SaveCount { get; private set; }

        public int TransactionCount { get; private set; }

        public List<int> SaveCountsByTransaction { get; } = [];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var role in roles.Items.Where(item => item.Id == 0))
            {
                SetId(role, _nextRoleId++);
            }

            foreach (var user in users.Items.Where(item => item.Id == 0))
            {
                SetId(user, _nextUserId++);
            }

            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task ExecuteSerializedTransactionAsync(
            string operationKey,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            Assert.Equal("ProvisionSystemAdministrator", operationKey);
            var initialSaveCount = SaveCount;
            TransactionCount++;
            await operation(cancellationToken);
            SaveCountsByTransaction.Add(SaveCount - initialSaveCount);
        }
    }
}
