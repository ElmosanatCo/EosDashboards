using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class PasswordResetTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Password_reset_commands_do_not_expose_passwords_or_tokens_in_diagnostics()
    {
        // Break caught: writing a recovery token or new password through exception/log formatting.
        var command = new CompletePasswordResetCommand("reset-token", "246810", "new password", "network");

        var text = command.ToString();

        Assert.DoesNotContain("reset-token", text, StringComparison.Ordinal);
        Assert.DoesNotContain("246810", text, StringComparison.Ordinal);
        Assert.DoesNotContain("new password", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reset_completion_consumes_a_reset_challenge_and_revokes_active_sessions()
    {
        // Break caught: issuing a session or retaining an active refresh credential after password recovery.
        var context = new PasswordResetContext();

        var result = await context.Complete.HandleAsync(
            new CompletePasswordResetCommand("reset-token", "246810", "new password", "network"),
            CancellationToken.None);

        Assert.Equal(PasswordResetStatus.Succeeded, result.Status);
        Assert.Equal(OtpChallengeStatus.Consumed, context.Challenge.Status);
        Assert.Equal("new-password-hash", context.User.PasswordHash);
        Assert.Equal(SessionRevocationReason.PasswordChanged, context.Session.RevocationReason);
    }

    [Fact]
    public async Task Known_user_starts_a_password_reset_purpose_challenge()
    {
        // Break caught: sending a recovery OTP that can also authenticate a user.
        var context = new PasswordResetContext();
        context.Challenge.Supersede();
        var start = new StartPasswordReset(
            context.Clock,
            context.Correlation,
            context.Users,
            context.Challenges,
            context.Sms,
            context.Secrets,
            context.Tokens,
            context.Mobile,
            context.Audit,
            context.UnitOfWork);

        var result = await start.HandleAsync(
            new StartPasswordResetCommand("local.user", "network"),
            CancellationToken.None);

        Assert.Equal(PasswordResetStartStatus.Succeeded, result.Status);
        Assert.Equal(OtpChallengePurpose.PasswordReset, Assert.Single(context.Challenges.Challenges, item => item.PublicToken == "reset-challenge-token").Purpose);
        var message = Assert.Single(context.Sms.Messages);
        Assert.Equal(
            $"داشبورد علم و صنعت، کد بازیابی رمز عبور شما: {context.Tokens.SixDigitCode}",
            message.Text);
    }

    [Fact]
    public async Task Password_reset_resend_after_sixty_seconds_replaces_only_its_own_challenge()
    {
        // Break caught: reusing a recovery OTP or making the visitor re-enter a password to request another one.
        var context = new PasswordResetContext();
        var start = context.CreateStart();

        var result = await start.ResendAsync(
            new ResendOtpCommand(context.Challenge.PublicToken, "network"),
            CancellationToken.None);

        Assert.Equal(PasswordResetStartStatus.Succeeded, result.Status);
        Assert.Equal("reset-challenge-token", result.ChallengeToken);
        Assert.Equal(OtpChallengeStatus.Superseded, context.Challenge.Status);
        Assert.Equal(2, context.Challenges.Challenges.Count);
        Assert.Single(context.Sms.Messages);
        AuditRecordAssertions.AssertSingle(context.Audit, null, 11, "PasswordResetOtpResent", true);
    }

    private sealed class PasswordResetContext
    {
        public PasswordResetContext()
        {
            User = User.Create("stable", "account", "Test", "User", "protected-mobile", "masked-mobile", Now);
            EntityId.Set(User, 11);
            User.SetLocalCredentials("LOCAL.USER", "old-password-hash", Now);
            Users.Users.Add(User);
            Challenge = OtpChallenge.Create(
                User.Id,
                "reset-token",
                "A1B2",
                Now,
                Now.AddMinutes(5),
                OtpChallengePurpose.PasswordReset);
            Challenge.MarkSent();
            EntityId.Set(Challenge, 101);
            Challenges.Challenges.Add(Challenge);
            Session = UserSession.Create(User.Id, "refresh-hash", Now);
            EntityId.Set(Session, 201);
            Sessions.Sessions.Add(Session);
            Secrets.Hashes["246810"] = "A1B2";
            Passwords.Hashes["new password"] = "new-password-hash";
            Tokens.SixDigitCode = "135790";
            Secrets.Hashes[Tokens.SixDigitCode] = "C3D4";
            Mobile.UnprotectedValues["protected-mobile"] = "synthetic-mobile";
            Tokens.AddOpaqueToken("reset-challenge-token");
            UnitOfWork = new FakeUnitOfWork(Challenges, Sessions);
            Complete = new CompletePasswordReset(
                Clock,
                Correlation,
                Users,
                Challenges,
                Sessions,
                Secrets,
                Passwords,
                Audit,
                UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now.AddMinutes(1));

        public FakeCorrelationContext Correlation { get; } = new("trace-test");

        public FakeUserRepository Users { get; } = new();

        public FakeOtpChallengeRepository Challenges { get; } = new();

        public FakeUserSessionRepository Sessions { get; } = new();

        public FakeSecretHasher Secrets { get; } = new();

        public FakePasswordHasher Passwords { get; } = new();

        public FakeSecureTokenGenerator Tokens { get; } = new();

        public FakeSmsSender Sms { get; } = new();

        public FakeMobileProtector Mobile { get; } = new();

        public FakeAuditWriter Audit { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; }

        public User User { get; }

        public OtpChallenge Challenge { get; }

        public UserSession Session { get; }

        public CompletePasswordReset Complete { get; }

        public StartPasswordReset CreateStart() => new(
            Clock,
            Correlation,
            Users,
            Challenges,
            Sms,
            Secrets,
            Tokens,
            Mobile,
            Audit,
            UnitOfWork);
    }
}
