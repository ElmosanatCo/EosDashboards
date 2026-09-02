using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class SessionLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Refresh_rotates_the_credential_and_does_not_extend_absolute_expiry()
    {
        // Break caught: retaining the old refresh credential or extending an eight-hour session during rotation.
        var context = new SessionContext();

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Succeeded, result.Status);
        Assert.Equal("replacement-refresh", result.RefreshCredential);
        Assert.Equal("C3D4", context.Session.RefreshCredentialHash);
        Assert.Equal(Now.AddHours(8), context.Session.ExpiresAtUtc);
        Assert.Equal(Now.AddHours(8), result.SessionExpiresAtUtc);
        Assert.Equal(context.Clock.UtcNow.AddMinutes(10), result.AccessToken?.ExpiresAtUtc);
        Assert.Equal([32], context.Tokens.RequestedByteCounts);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.Null(await context.Sessions.FindByRefreshHashAsync("A1B2", CancellationToken.None));
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "SessionRefreshed", true);
    }

    [Fact]
    public async Task Refresh_at_absolute_expiry_is_denied_without_rotation()
    {
        // Break caught: refreshing at the exact eight-hour absolute expiry boundary.
        var context = new SessionContext();
        context.Clock.UtcNow = Now.AddHours(8);

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Denied, result.Status);
        Assert.Equal("A1B2", context.Session.RefreshCredentialHash);
        Assert.Empty(context.TokenIssuer.Requests);
        Assert.Empty(context.Tokens.RequestedByteCounts);
    }

    [Fact]
    public async Task Refresh_with_one_tick_less_than_ten_minutes_remaining_is_denied_without_rotation()
    {
        // Break caught: issuing an access token whose lifetime extends beyond the absolute session expiry.
        var context = new SessionContext();
        context.Clock.UtcNow = Now.AddHours(8).AddMinutes(-10).AddTicks(1);

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Denied, result.Status);
        Assert.Equal("A1B2", context.Session.RefreshCredentialHash);
        Assert.Empty(context.TokenIssuer.Requests);
        Assert.Empty(context.Tokens.RequestedByteCounts);
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "SessionRefreshDenied", false);
    }

    [Fact]
    public async Task Refresh_with_exactly_ten_minutes_remaining_is_allowed_and_token_ends_with_session()
    {
        // Break caught: denying the exact valid boundary or allowing its access token beyond the session.
        var context = new SessionContext();
        context.Clock.UtcNow = Now.AddHours(8).AddMinutes(-10);

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Succeeded, result.Status);
        Assert.Equal(Now.AddHours(8), result.AccessToken?.ExpiresAtUtc);
        Assert.Equal(Now.AddHours(8), result.SessionExpiresAtUtc);
        Assert.Equal("C3D4", context.Session.RefreshCredentialHash);
    }

    [Fact]
    public async Task Revoked_session_is_denied_without_rotation()
    {
        // Break caught: accepting a refresh credential after logout or administrative revocation.
        var context = new SessionContext();
        context.Session.Revoke(SessionRevocationReason.Administrator, Now.AddMinutes(30));

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Denied, result.Status);
        Assert.Equal("A1B2", context.Session.RefreshCredentialHash);
        Assert.Empty(context.TokenIssuer.Requests);
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "SessionRefreshDenied", false);
    }

    [Fact]
    public async Task Refresh_with_resolved_session_and_missing_user_preserves_session_audit_attribution()
    {
        // Break caught: losing the known session subject when the associated user row cannot be loaded.
        var context = new SessionContext();
        context.Users.Users.Clear();

        var result = await context.Refresh.HandleAsync(
            new RefreshSessionCommand("current-refresh"),
            CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Denied, result.Status);
        Assert.Equal("A1B2", context.Session.RefreshCredentialHash);
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "SessionRefreshDenied", false);
    }

    [Fact]
    public async Task Logout_is_idempotent_and_preserves_the_first_user_logout_revocation()
    {
        // Break caught: repeated logout changing revocation evidence or failing after the first request.
        var context = new SessionContext();

        await context.Logout.HandleAsync(new LogoutCommand(context.Session.Id), CancellationToken.None);
        context.Clock.UtcNow = Now.AddHours(2);
        await context.Logout.HandleAsync(new LogoutCommand(context.Session.Id), CancellationToken.None);

        Assert.Equal(Now.AddHours(1), context.Session.RevokedAtUtc);
        Assert.Equal(SessionRevocationReason.UserLogout, context.Session.RevocationReason);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "UserLogout", true);
    }

    private sealed class SessionContext
    {
        public SessionContext()
        {
            User = User.Create(
                "stable-user",
                "DOMAIN\\user",
                "Test",
                "User",
                "protected-mobile",
                "masked-mobile",
                Now.AddDays(-1));
            EntityId.Set(User, 11);
            Users.Users.Add(User);
            Session = UserSession.Create(User.Id, "A1B2", Now);
            EntityId.Set(Session, 201);
            Sessions.Sessions.Add(Session);
            Hasher.Hashes["current-refresh"] = "A1B2";
            Hasher.Hashes["replacement-refresh"] = "C3D4";
            Tokens.AddOpaqueToken("replacement-refresh");
            UnitOfWork = new FakeUnitOfWork(OtpChallenges, Sessions);
            Refresh = new RefreshSession(
                Clock,
                Correlation,
                Users,
                Sessions,
                Hasher,
                Tokens,
                TokenIssuer,
                Audit,
                UnitOfWork);
            Logout = new Logout(Clock, Correlation, Sessions, Audit, UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now.AddHours(1));

        public FakeCorrelationContext Correlation { get; } = new("trace-test");

        public FakeUserRepository Users { get; } = new();

        public FakeOtpChallengeRepository OtpChallenges { get; } = new();

        public FakeUserSessionRepository Sessions { get; } = new();

        public FakeSecretHasher Hasher { get; } = new();

        public FakeSecureTokenGenerator Tokens { get; } = new();

        public FakeAccessTokenIssuer TokenIssuer { get; } = new();

        public FakeAuditWriter Audit { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; }

        public RefreshSession Refresh { get; }

        public Logout Logout { get; }

        public User User { get; }

        public UserSession Session { get; }
    }
}
