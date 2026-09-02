using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Tests;

public sealed class UserSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_sets_an_eight_hour_absolute_expiry()
    {
        // Break caught: creating a session with a lifetime other than eight absolute hours.
        var session = UserSession.Create(1, "AABB", Now);

        Assert.Equal(Now.AddHours(8), session.ExpiresAtUtc);
        Assert.True(session.IsActive(Now.AddHours(8).AddTicks(-1)));
        Assert.False(session.IsActive(Now.AddHours(8)));
    }

    [Fact]
    public void Rotate_replaces_refresh_hash_without_extending_absolute_expiry()
    {
        // Break caught: retaining a replaced refresh credential or extending the session on rotation.
        var session = UserSession.Create(1, "AABB", Now);

        session.Rotate("CCDD", Now.AddHours(1));

        Assert.Equal("CCDD", session.RefreshCredentialHash);
        Assert.Equal(Now.AddHours(1), session.LastRefreshedAtUtc);
        Assert.Equal(Now.AddHours(8), session.ExpiresAtUtc);
        Assert.False(session.IsActive(Now.AddHours(8)));
    }

    [Fact]
    public void Revoke_is_idempotent()
    {
        // Break caught: changing the original revocation audit record on repeated logout.
        var session = UserSession.Create(1, "AABB", Now);

        session.Revoke(SessionRevocationReason.UserLogout, Now.AddMinutes(1));
        session.Revoke(SessionRevocationReason.Administrator, Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(1), session.RevokedAtUtc);
        Assert.Equal(SessionRevocationReason.UserLogout, session.RevocationReason);
        Assert.False(session.IsActive(Now.AddMinutes(2)));
    }
}
