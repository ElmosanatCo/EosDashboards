using System.Reflection;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Administration;
using EosDashboards.Application.Tests.Auth;
using EosDashboards.Domain.Authorization;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Administration;

public sealed class ManageUsersTests
{
    private static readonly DateTime Now = new(2026, 9, 3, 9, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task Create_defaults_username_to_personnel_code_and_requires_a_password_change()
    {
        var context = new ManageUsersContext();

        var result = await context.UseCase.CreateAsync(
            context.Actor.Id,
            new CreateUserCommand(
                "124",
                "نام",
                "خانوادگی",
                "09121111111",
                null,
                "temporary-password",
                context.Department.Id,
                [context.DepartmentManagerRole.Id]),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.Succeeded, result.Status);
        var user = Assert.IsType<User>(result.User);
        Assert.Equal("124", user.Username);
        Assert.True(user.MustChangePassword);
        Assert.Equal("UserCreated", Assert.Single(context.Audit.Records).EventCode);
        Assert.Equal(context.Actor.Id, context.Audit.Records[0].ActorUserId);
        Assert.Equal(user.Id, context.Audit.Records[0].SubjectUserId);
        Assert.Equal("protected:09121111111", user.ProtectedMobileNumber);
        Assert.Equal("09******111", user.MaskedMobileNumber);
    }

    [Fact]
    public async Task Update_replaces_mobile_and_revokes_other_user_sessions()
    {
        var context = new ManageUsersContext();
        var target = context.AddUser("125", [context.DepartmentManagerRole.Id]);
        var session = context.AddSession(target.Id);

        var result = await context.UseCase.UpdateAsync(
            context.Actor.Id,
            new UpdateUserCommand(
                target.Id,
                "126",
                "نام جدید",
                "نام خانوادگی جدید",
                "09123333333",
                "custom.username",
                context.Department.Id,
                [context.HumanResourcesRole.Id],
                target.RowVersion),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.Succeeded, result.Status);
        Assert.Equal("126", target.OrganizationalId);
        Assert.Equal("CUSTOM.USERNAME", target.Username);
        Assert.Equal("protected:09123333333", target.ProtectedMobileNumber);
        Assert.Equal(SessionRevocationReason.AdministrativeChange, session.RevocationReason);
        Assert.Contains(context.Audit.Records, record => record.EventCode == "UserUpdated");
        Assert.Contains(context.Audit.Records, record => record.EventCode == "UserRolesChanged");
    }

    [Fact]
    public async Task Deactivating_the_last_active_system_administrator_is_rejected_without_audit_success()
    {
        var context = new ManageUsersContext();

        var result = await context.UseCase.SetActiveAsync(
            context.Actor.Id,
            new SetUserActiveCommand(context.Actor.Id, false, context.Actor.RowVersion),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.LastSystemAdministrator, result.Status);
        Assert.True(context.Actor.IsActive);
        Assert.Empty(context.Audit.Records);
    }

    [Fact]
    public async Task Reset_password_sets_temporary_password_and_keeps_the_actor_current_session()
    {
        var context = new ManageUsersContext();
        var target = context.AddUser("125", [context.DepartmentManagerRole.Id]);
        var actorSession = context.AddSession(context.Actor.Id);
        var targetSession = context.AddSession(target.Id);

        var result = await context.UseCase.ResetPasswordAsync(
            context.Actor.Id,
            new ResetUserPasswordCommand(target.Id, "temporary-password", target.RowVersion),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.Succeeded, result.Status);
        Assert.True(target.MustChangePassword);
        Assert.Equal("temporary-hash", target.PasswordHash);
        Assert.Null(actorSession.RevocationReason);
        Assert.Equal(SessionRevocationReason.AdministrativeChange, targetSession.RevocationReason);
        Assert.Equal("UserPasswordReset", Assert.Single(context.Audit.Records).EventCode);
    }

    [Fact]
    public async Task Removing_the_system_administrator_role_from_the_last_active_administrator_is_rejected()
    {
        var context = new ManageUsersContext();

        var result = await context.UseCase.UpdateAsync(
            context.Actor.Id,
            new UpdateUserCommand(
                context.Actor.Id,
                context.Actor.OrganizationalId,
                context.Actor.FirstName,
                context.Actor.LastName,
                null,
                context.Actor.Username!,
                context.Department.Id,
                [context.DepartmentManagerRole.Id],
                context.Actor.RowVersion),
            CancellationToken.None);

        Assert.Equal(ManageUserStatus.LastSystemAdministrator, result.Status);
        Assert.Contains(context.Actor.UserRoles, role => role.RoleId == context.SystemAdministratorRole.Id);
        Assert.Empty(context.Audit.Records);
    }

    private sealed class ManageUsersContext
    {
        public ManageUsersContext()
        {
            EntityId.Set(SystemAdministratorRole, 1);
            EntityId.Set(DepartmentManagerRole, 2);
            EntityId.Set(HumanResourcesRole, 3);
            Roles.Roles.AddRange([SystemAdministratorRole, DepartmentManagerRole, HumanResourcesRole]);
            EntityId.Set(Department, 1);
            Departments.Departments.Add(Department);
            Actor = AddUser("123", [SystemAdministratorRole.Id, DepartmentManagerRole.Id]);
            Actor.SetLocalCredentials("admin", "admin-hash", Now);
            SetRowVersion(Actor);
            Passwords.Hashes["temporary-password"] = "temporary-hash";
            UnitOfWork = new FakeUnitOfWork(Challenges, Sessions);
            UseCase = new ManageUsers(
                Clock,
                Correlation,
                Users,
                Roles,
                Departments,
                Sessions,
                Mobile,
                Passwords,
                Audit,
                UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now);
        public FakeCorrelationContext Correlation { get; } = new("trace-test");
        public FakeUserRepository Users { get; } = new();
        public FakeRoleRepository Roles { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeUserSessionRepository Sessions { get; } = new();
        public FakeOtpChallengeRepository Challenges { get; } = new();
        public FakePasswordHasher Passwords { get; } = new();
        public FakeAuditWriter Audit { get; } = new();
        public TestMobileProtector Mobile { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public ManageUsers UseCase { get; }
        public Role SystemAdministratorRole { get; } = Role.Create(SystemRoleCodes.SystemAdministrator, "مدیر سامانه", true, Now);
        public Role DepartmentManagerRole { get; } = Role.Create(SystemRoleCodes.DepartmentManager, "مدیر بخش", true, Now);
        public Role HumanResourcesRole { get; } = Role.Create(SystemRoleCodes.HumanResourcesManager, "مدیر منابع انسانی", true, Now);
        public Department Department { get; } = Department.CreateRoot("واحد آزمایشی", Now);
        public User Actor { get; }

        public User AddUser(string personnelCode, IReadOnlyCollection<long> roleIds)
        {
            var user = User.Create(
                personnelCode,
                "نام",
                "خانوادگی",
                "protected:09120000000",
                "09******000",
                Department.Id,
                Now);
            EntityId.Set(user, Users.Users.Count + 11);
            foreach (var roleId in roleIds)
            {
                user.AssignRole(roleId);
            }

            user.SetLocalCredentials($"USER{personnelCode}", "existing-hash", Now);
            SetRowVersion(user);
            Users.Users.Add(user);
            return user;
        }

        public UserSession AddSession(long userId)
        {
            var session = UserSession.Create(userId, $"refresh-{Sessions.Sessions.Count + 1}", Now.AddMinutes(-1));
            EntityId.Set(session, Sessions.Sessions.Count + 201);
            Sessions.Sessions.Add(session);
            return session;
        }

        private static void SetRowVersion(User user) =>
            typeof(User).GetProperty(nameof(User.RowVersion), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(user, new byte[] { 1, 2, 3, 4 });
    }

    private sealed class TestMobileProtector : IMobileProtector
    {
        public string Protect(string normalizedMobile) => $"protected:{normalizedMobile}";
        public string Unprotect(string protectedMobile) => throw new NotSupportedException();
        public string Mask(string normalizedMobile) => $"{normalizedMobile[..2]}******{normalizedMobile[^3..]}";
    }
}
