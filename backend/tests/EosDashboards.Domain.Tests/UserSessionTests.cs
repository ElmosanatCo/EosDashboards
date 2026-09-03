using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Tests;

public sealed class UserSessionTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Create_preserves_local_millisecond_expiry()
    {
        // Break caught: retaining UTC-normalized session expiry values.
        var createdAt = new DateTime(2026, 9, 3, 18, 30, 15, 123, DateTimeKind.Unspecified);
        var session = UserSession.Create(1, "AABB", createdAt);

        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(createdAt.AddHours(8), session.ExpiresAt);
    }

    [Fact]
    public void Create_sets_an_eight_hour_absolute_expiry()
    {
        // Break caught: creating a session with a lifetime other than eight absolute hours.
        var session = UserSession.Create(1, "AABB", Now);

        Assert.Equal(Now.AddHours(8), session.ExpiresAt);
        Assert.True(session.IsActive(Now.AddHours(8).AddTicks(-1)));
        Assert.False(session.IsActive(Now.AddHours(8)));
    }

    [Fact]
    public void Is_active_rejects_an_instant_before_session_creation()
    {
        // Break caught: accepting a session before its recorded creation time.
        var session = UserSession.Create(1, "AABB", Now);

        Assert.False(session.IsActive(Now.AddTicks(-1)));
    }

    [Fact]
    public void Rotate_replaces_refresh_hash_without_extending_absolute_expiry()
    {
        // Break caught: retaining a replaced refresh credential or extending the session on rotation.
        var session = UserSession.Create(1, "AABB", Now);

        session.Rotate("CCDD", Now.AddHours(1));

        Assert.Equal("CCDD", session.RefreshCredentialHash);
        Assert.Equal(Now.AddHours(1), session.LastRefreshedAt);
        Assert.Equal(Now.AddHours(8), session.ExpiresAt);
        Assert.False(session.IsActive(Now.AddHours(8)));
    }

    [Fact]
    public void Rotate_rejects_the_existing_refresh_hash()
    {
        // Break caught: accepting refresh rotation that does not invalidate the current credential.
        var session = UserSession.Create(1, "AABB", Now);

        Assert.Throws<ArgumentException>(() => session.Rotate("AABB", Now.AddHours(1)));

        Assert.Equal("AABB", session.RefreshCredentialHash);
        Assert.Null(session.LastRefreshedAt);
        Assert.Equal(Now.AddHours(8), session.ExpiresAt);
    }

    [Fact]
    public void Revoke_is_idempotent()
    {
        // Break caught: changing the original revocation audit record on repeated logout.
        var session = UserSession.Create(1, "AABB", Now);

        session.Revoke(SessionRevocationReason.UserLogout, Now.AddMinutes(1));
        session.Revoke(SessionRevocationReason.Administrator, Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(1), session.RevokedAt);
        Assert.Equal(SessionRevocationReason.UserLogout, session.RevocationReason);
        Assert.False(session.IsActive(Now.AddMinutes(2)));
    }

    [Fact]
    public void Password_changed_is_a_distinct_session_revocation_reason()
    {
        // Break caught: losing the security reason when password changes invalidate sessions.
        var session = UserSession.Create(1, "AABB", Now);

        session.Revoke(SessionRevocationReason.PasswordChanged, Now.AddMinutes(1));

        Assert.Equal(SessionRevocationReason.PasswordChanged, session.RevocationReason);
    }

    [Fact]
    public void Administrative_change_is_a_distinct_session_revocation_reason()
    {
        // Break caught: losing the security reason when an administrator changes a target account.
        var session = UserSession.Create(1, "AABB", Now);

        session.Revoke(SessionRevocationReason.AdministrativeChange, Now.AddMinutes(1));

        Assert.Equal(SessionRevocationReason.AdministrativeChange, session.RevocationReason);
    }
}
