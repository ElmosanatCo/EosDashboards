using EosDashboards.Domain.Entities;

namespace EosDashboards.Domain.Tests;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

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
    public void Deactivate_makes_user_inactive()
    {
        // Break caught: allowing a deactivated user to remain eligible for sign-in.
        var user = CreateUser();

        user.Deactivate(Now.AddMinutes(1));

        Assert.False(user.IsActive);
        Assert.Equal(Now.AddMinutes(1), user.DeactivatedAtUtc);
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
        Assert.Equal(Now.AddMinutes(1), user.UpdatedAtUtc);
    }

    [Fact]
    public void Role_create_rejects_missing_stable_code()
    {
        // Break caught: persisting a role that cannot be referenced by a stable authorization code.
        Assert.Throws<ArgumentException>(() => Role.Create(" ", "مدیر سامانه", true, Now));
    }

    [Fact]
    public void Preference_create_normalizes_its_timestamp_to_utc()
    {
        // Break caught: persisting a user preference timestamp with a local offset.
        var preference = UserPreference.Create(1, "system", "navyTeal", false, Now.ToOffset(TimeSpan.FromHours(3.5)));

        Assert.Equal(TimeSpan.Zero, preference.CreatedAtUtc.Offset);
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
            Now);
    }
}
