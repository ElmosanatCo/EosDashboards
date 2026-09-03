using System.Reflection;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Provisioning;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Provisioning;

public sealed class ProvisionSystemAdministratorTests
{
    private static readonly DateTime TestNow =
        new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Unspecified);

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
        var systemAdministratorRole = Assert.Single(dependencies.Roles.Items, role => role.Code == "SystemAdministrator");
        var departmentManagerRole = Assert.Single(dependencies.Roles.Items, role => role.Code == "DepartmentManager");
        Assert.True(user.IsActive);
        Assert.Equal("ORG-SYNTHETIC-7", user.OrganizationalId);
        Assert.Equal("DOMAIN\\SYNTHETIC.TWO", user.AccountName);
        Assert.Equal("LOCAL.ADMIN.TWO", user.Username);
        Assert.Equal("hash:second synthetic password", user.PasswordHash);
        Assert.Equal("SyntheticFirstTwo", user.FirstName);
        Assert.Equal("SyntheticLastTwo", user.LastName);
        Assert.Equal("protected-mobile-two", user.ProtectedMobileNumber);
        Assert.Equal("*******6789", user.MaskedMobileNumber);
        Assert.Equal("SystemAdministrator", systemAdministratorRole.Code);
        Assert.Equal("مدیر سامانه", systemAdministratorRole.DisplayName);
        Assert.True(systemAdministratorRole.IsSystem);
        Assert.True(systemAdministratorRole.IsActive);
        Assert.Equal("DepartmentManager", departmentManagerRole.Code);
        Assert.Equal("مدیر بخش", departmentManagerRole.DisplayName);
        Assert.Equal(1, user.DepartmentId);
        Assert.Equal(2, user.UserRoles.Count);
        Assert.Contains(user.UserRoles, assignment => assignment.RoleId == systemAdministratorRole.Id);
        Assert.Contains(user.UserRoles, assignment => assignment.RoleId == departmentManagerRole.Id);
        Assert.True(user.Id > 0);
        Assert.True(systemAdministratorRole.Id > 0);
        Assert.True(departmentManagerRole.Id > 0);

        Assert.Equal(user.Id, firstResult.UserId);
        Assert.Equal(user.Id, secondResult.UserId);
        Assert.Equal("*******6789", firstResult.MaskedMobile);
        Assert.Equal("*******6789", secondResult.MaskedMobile);
        Assert.Equal(2, dependencies.UnitOfWork.TransactionCount);
        Assert.Equal([2, 1], dependencies.UnitOfWork.SaveCountsByTransaction);
        Assert.Empty(dependencies.ExternalIdentityLinks.Items);

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
            1,
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
        Assert.Null(user.DeactivatedAt);
        Assert.Equal(2, user.UserRoles.Count);
        Assert.Equal([2], dependencies.UnitOfWork.SaveCountsByTransaction);
    }

    [Fact]
    public async Task RepeatedProvisioningUpdatesThePendingGoogleEmailForTheSystemAdministrator()
    {
        var dependencies = new ProvisioningDependencies();
        var sut = dependencies.CreateSut();

        await sut.HandleAsync(
            new ProvisionSystemAdministratorCommand(
                "org-synthetic-google",
                "domain\\synthetic.google",
                "local.google",
                "synthetic password",
                "Synthetic",
                "Google",
                "09120006789",
                "  first.synthetic@example.test  "),
            CancellationToken.None);
        var firstLink = Assert.Single(dependencies.ExternalIdentityLinks.Items);
        firstLink.BindSubject("synthetic-google-subject", TestNow.AddMinutes(1));
        await sut.HandleAsync(
            new ProvisionSystemAdministratorCommand(
                "org-synthetic-google",
                "domain\\synthetic.google",
                "local.google",
                "synthetic password",
                "Synthetic",
                "Google",
                "09120006789",
                "second.synthetic@example.test"),
            CancellationToken.None);

        var link = Assert.Single(dependencies.ExternalIdentityLinks.Items);
        var user = Assert.Single(dependencies.Users.Items);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(ExternalIdentityProvider.Google, link.Provider);
        Assert.Equal("SECOND.SYNTHETIC@EXAMPLE.TEST", link.NormalizedEmail);
        Assert.Equal("synthetic-google-subject", link.ProviderSubject);
        Assert.Equal(TestNow.AddMinutes(1), link.LinkedAt);
        Assert.DoesNotContain(
            "second.synthetic@example.test",
            string.Join(Environment.NewLine, dependencies.Audits.Records),
            StringComparison.OrdinalIgnoreCase);
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

        Assert.Equal("A fixed system role is not valid.", exception.Message);
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

        public TestDepartmentRepository Departments { get; } = new();

        public TestMobileProtector MobileProtector { get; } = new();

        public TestPasswordHasher PasswordHasher { get; } = new();

        public TestAuditWriter Audits { get; } = new();

        public TestExternalIdentityLinkRepository ExternalIdentityLinks { get; } = new();

        public TestUnitOfWork UnitOfWork { get; }

        public ProvisioningDependencies()
        {
            UnitOfWork = new TestUnitOfWork(Users, Roles, ExternalIdentityLinks);
        }

        public ProvisionSystemAdministrator CreateSut() => new(
            new TestClock(TestNow),
            new TestCorrelationContext(),
            Users,
            Roles,
            Departments,
            MobileProtector,
            PasswordHasher,
            Audits,
            ExternalIdentityLinks,
            UnitOfWork);
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime Now { get; } = utcNow;
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

        public Task<User?> GetForUpdateAsync(long id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<int> CountActiveWithRoleAsync(long roleId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.Count(item =>
                item.IsActive && item.UserRoles.Any(userRole => userRole.RoleId == roleId)));
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

        public Task<IReadOnlyList<Role>> GetByIdsAsync(
            IReadOnlyCollection<long> ids,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Role>>(Items.Where(role => ids.Contains(role.Id)).ToArray());
        }
    }

    private sealed class TestDepartmentRepository : IDepartmentRepository
    {
        public TestDepartmentRepository()
        {
            var department = Department.CreateRoot("نرم افزار", TestNow);
            SetId(department, 1);
            Items.Add(department);
        }

        public List<Department> Items { get; } = [];

        public Task<Department?> FindByNameAsync(string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(department => department.Name == name));
        }

        public Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(department => department.Id == id));
        }
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

    private sealed class TestExternalIdentityLinkRepository : IExternalIdentityLinkRepository
    {
        public List<ExternalIdentityLink> Items { get; } = [];

        public Task<ExternalIdentityLink?> FindByProviderSubjectAsync(
            ExternalIdentityProvider provider,
            string providerSubject,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item =>
                item.Provider == provider && item.ProviderSubject == providerSubject));
        }

        public Task<ExternalIdentityLink?> FindPendingByProviderEmailAsync(
            ExternalIdentityProvider provider,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item =>
                item.Provider == provider &&
                item.ProviderSubject is null &&
                item.NormalizedEmail == normalizedEmail));
        }

        public Task<ExternalIdentityLink?> FindByUserIdAndProviderAsync(
            long userId,
            ExternalIdentityProvider provider,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item =>
                item.UserId == userId && item.Provider == provider));
        }

        public void Add(ExternalIdentityLink link) => Items.Add(link);
    }

    private sealed class TestUnitOfWork(
        TestUserRepository users,
        TestRoleRepository roles,
        TestExternalIdentityLinkRepository externalIdentityLinks) : IUnitOfWork
    {
        private long _nextUserId = 101;
        private long _nextRoleId = 201;
        private long _nextExternalIdentityLinkId = 301;

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

            foreach (var externalIdentityLink in externalIdentityLinks.Items.Where(item => item.Id == 0))
            {
                SetId(externalIdentityLink, _nextExternalIdentityLinkId++);
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
