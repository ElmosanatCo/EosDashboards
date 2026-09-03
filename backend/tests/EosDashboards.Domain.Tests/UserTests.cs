using EosDashboards.Domain.Entities;

namespace EosDashboards.Domain.Tests;

public sealed class UserTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Create_rejects_missing_stable_organizational_id()
    {
        // Break caught: permitting a user without the immutable organizational identity used for lookup.
        Assert.Throws<ArgumentException>(() => User.Create(
            " ",
            "account",
            "First",
            "Last",
            "protected-mobile",
            "masked-mobile",
            1,
            Now));
    }

    [Fact]
    public void Assign_role_is_idempotent()
    {
        // Break caught: duplicate role links granting the same role more than once.
        var user = CreateUser();

        user.AssignRole(7);
        user.AssignRole(7);

        var assignment = Assert.Single(user.UserRoles);
        Assert.Equal(7, assignment.RoleId);
    }

    [Fact]
    public void Create_requires_a_positive_department_identifier()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => User.Create(
            "stable-organizational-id",
            "account",
            "First",
            "Last",
            "protected-mobile",
            "masked-mobile",
            0,
            Now));
    }

    [Fact]
    public void Deactivate_makes_user_inactive()
    {
        // Break caught: allowing a deactivated user to remain eligible for sign-in.
        var user = CreateUser();

        user.Deactivate(Now.AddMinutes(1));

        Assert.False(user.IsActive);
        Assert.Equal(Now.AddMinutes(1), user.DeactivatedAt);
    }

    [Fact]
    public void Update_profile_replaces_current_directory_and_mobile_data()
    {
        // Break caught: retaining stale display or mobile data after a profile update.
        var user = CreateUser();

        user.UpdateProfile("new-account", "Updated", "Person", "new-protected-mobile", "new-masked-mobile", Now.AddMinutes(1));

        Assert.Equal("new-account", user.AccountName);
        Assert.Equal("Updated", user.FirstName);
        Assert.Equal("Person", user.LastName);
        Assert.Equal("new-protected-mobile", user.ProtectedMobileNumber);
        Assert.Equal("new-masked-mobile", user.MaskedMobileNumber);
        Assert.Equal(Now.AddMinutes(1), user.UpdatedAt);
    }

    [Fact]
    public void Set_local_credentials_normalizes_username_and_preserves_password_hash()
    {
        // Break caught: accepting ambiguous usernames or changing an opaque password hash.
        var user = CreateUser();

        user.SetLocalCredentials("  Admin.User  ", "versioned-password-hash", Now.AddMinutes(1));

        Assert.Equal("ADMIN.USER", user.Username);
        Assert.Equal("versioned-password-hash", user.PasswordHash);
        Assert.Equal(Now.AddMinutes(1), user.UpdatedAt);
    }

    [Fact]
    public void Set_local_credentials_rejects_missing_username_or_password_hash()
    {
        // Break caught: creating a locally sign-in-capable account without a stable credential pair.
        var user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.SetLocalCredentials(" ", "hash", Now));
        Assert.Throws<ArgumentException>(() => user.SetLocalCredentials("admin", " ", Now));
    }

    [Fact]
    public void Temporary_local_credentials_require_a_password_change_until_completed()
    {
        // Break caught: allowing an administrator-set temporary password to open the workspace unchanged.
        var user = CreateUser();

        user.SetTemporaryLocalCredentials("  124  ", "temporary-password-hash", Now);

        Assert.Equal("124", user.Username);
        Assert.True(user.MustChangePassword);

        user.CompleteTemporaryPasswordChange("replacement-password-hash", Now.AddMinutes(1));

        Assert.False(user.MustChangePassword);
        Assert.Equal("replacement-password-hash", user.PasswordHash);
    }

    [Fact]
    public void Replace_roles_removes_omitted_roles_and_rejects_an_empty_assignment_for_an_active_user()
    {
        // Break caught: retaining a role after an administrator removes it, or leaving an active account without access.
        var user = CreateUser();
        user.AssignRole(7);
        user.AssignRole(11);

        user.ReplaceRoles([11, 13], Now.AddMinutes(1));

        Assert.Equal([11L, 13L], user.UserRoles.Select(role => role.RoleId).Order());
        Assert.Throws<ArgumentException>(() => user.ReplaceRoles([], Now.AddMinutes(2)));
    }

    [Fact]
    public void Update_organizational_id_replaces_the_personnel_code()
    {
        // Break caught: preventing correction of a recorded personnel code.
        var user = CreateUser();

        user.UpdateOrganizationalId("corrected-personnel-code", Now.AddMinutes(1));

        Assert.Equal("corrected-personnel-code", user.OrganizationalId);
        Assert.Equal(Now.AddMinutes(1), user.UpdatedAt);
    }

    [Fact]
    public void Activate_rejects_a_user_without_any_role()
    {
        // Break caught: reactivating an account that cannot access any approved workspace.
        var user = CreateUser();
        user.Deactivate(Now);

        Assert.Throws<InvalidOperationException>(() => user.Activate(Now.AddMinutes(1)));
    }

    [Fact]
    public void Role_create_rejects_missing_stable_code()
    {
        // Break caught: persisting a role that cannot be referenced by a stable authorization code.
        Assert.Throws<ArgumentException>(() => Role.Create(" ", "مدیر سامانه", true, Now));
    }

    [Fact]
    public void Preference_create_preserves_its_local_timestamp()
    {
        // Break caught: converting an application-local preference timestamp before persistence.
        var createdAt = new DateTime(2026, 9, 2, 11, 30, 0, 123, DateTimeKind.Unspecified);
        var preference = UserPreference.Create(1, "system", "navyTeal", false, createdAt);

        Assert.Equal(createdAt, preference.CreatedAt);
    }

    [Fact]
    public void Audit_log_create_rejects_nonpositive_subject_identifier()
    {
        // Break caught: creating an audit record that references an invalid subject.
        Assert.Throws<ArgumentOutOfRangeException>(() => AuditLog.Create(
            null,
            0,
            "AuthenticationSucceeded",
            Now,
            true,
            "trace-id",
            null));
    }

    private static User CreateUser()
    {
        return User.Create(
            "stable-organizational-id",
            "account",
            "First",
            "Last",
            "protected-mobile",
            "masked-mobile",
            1,
            Now);
    }
}
