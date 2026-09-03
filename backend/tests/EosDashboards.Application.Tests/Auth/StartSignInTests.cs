using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class StartSignInTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Known_active_user_sends_one_sms_and_persists_a_sent_challenge()
    {
        // Break caught: issuing a public challenge without securely persisting and delivering its OTP.
        var context = new StartSignInContext();

        var result = await context.UseCase.HandleAsync(context.Command, CancellationToken.None);

        Assert.Equal(StartSignInStatus.Succeeded, result.Status);
        Assert.Equal("challenge-token", result.ChallengeToken);
        Assert.Equal("masked-mobile", result.MaskedMobile);
        Assert.Equal(Now.AddMinutes(5), result.ExpiresAtUtc);
        Assert.Equal(Now.AddSeconds(60), result.ResendAvailableAtUtc);
        var message = Assert.Single(context.Sms.Messages);
        Assert.Equal("synthetic-normalized-mobile", message.Mobile);
        Assert.Contains(context.Tokens.SixDigitCode, message.Text, StringComparison.Ordinal);
        var challenge = Assert.Single(context.OtpChallenges.Challenges);
        Assert.Equal("A1B2", challenge.CodeHash);
        Assert.Equal(OtpChallengeStatus.Sent, challenge.Status);
        Assert.Equal([32], context.Tokens.RequestedByteCounts);
        Assert.Equal(2, context.UnitOfWork.SaveCount);
        Assert.Equal(OtpChallengeStatus.Pending, Assert.Single(context.UnitOfWork.Observations[0].ChallengeStatuses));
        Assert.Equal(OtpChallengeStatus.Sent, Assert.Single(context.UnitOfWork.Observations[1].ChallengeStatuses));
        AuditRecordAssertions.AssertSingle(context.Audit, null, 11, "OtpSent", true);
        Assert.DoesNotContain(context.Tokens.SixDigitCode, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(message.Mobile, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_and_inactive_users_receive_the_same_public_denial()
    {
        // Break caught: revealing whether a denied organizational identity exists in the application database.
        var unknown = new StartSignInContext(addUser: false);
        var inactive = new StartSignInContext();
        inactive.User!.Deactivate(Now.AddMinutes(-1));

        var unknownResult = await unknown.UseCase.HandleAsync(unknown.Command, CancellationToken.None);
        var inactiveResult = await inactive.UseCase.HandleAsync(inactive.Command, CancellationToken.None);

        Assert.Equal(unknownResult, inactiveResult);
        Assert.Equal(StartSignInStatus.Denied, unknownResult.Status);
        Assert.Empty(unknown.Sms.Messages);
        Assert.Empty(inactive.Sms.Messages);
        AuditRecordAssertions.AssertSingle(unknown.Audit, null, null, "SignInDenied", false);
        AuditRecordAssertions.AssertSingle(inactive.Audit, null, 11, "SignInDenied", false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sms_failure_or_timeout_marks_the_challenge_failed_and_returns_dependency_unavailable(bool timeout)
    {
        // Break caught: leaving a usable challenge or creating a retry opportunity after ambiguous delivery failure.
        var context = new StartSignInContext();
        if (timeout)
        {
            context.Sms.Exception = new TimeoutException("synthetic timeout");
        }
        else
        {
            context.Sms.Result = new SmsSendResult(false, "provider-unavailable");
        }

        var result = await context.UseCase.HandleAsync(context.Command, CancellationToken.None);

        Assert.Equal(StartSignInStatus.DependencyUnavailable, result.Status);
        Assert.Null(result.ChallengeToken);
        Assert.Equal(OtpChallengeStatus.SendFailed, Assert.Single(context.OtpChallenges.Challenges).Status);
        Assert.Single(context.Sms.Messages);
        Assert.Equal(2, context.UnitOfWork.SaveCount);
        var audit = Assert.Single(context.Audit.Records, record => record.EventCode == "OtpSendFailed");
        Assert.Null(audit.ActorUserId);
        Assert.Equal(11, audit.SubjectUserId);
        Assert.Equal("trace-test", audit.TraceId);
        Assert.False(audit.Succeeded);
        Assert.DoesNotContain(context.Tokens.SixDigitCode, audit.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(context.Sms.Messages[0].Mobile, audit.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Challenge_created_less_than_sixty_seconds_ago_enforces_cooldown()
    {
        // Break caught: sending a replacement OTP before the exact 60-second resend boundary.
        var context = new StartSignInContext();
        context.Clock.UtcNow = Now.AddSeconds(60).AddTicks(-1);
        context.AddExistingChallenge(Now);

        var result = await context.UseCase.HandleAsync(context.Command, CancellationToken.None);

        Assert.Equal(StartSignInStatus.Cooldown, result.Status);
        Assert.Equal(Now.AddSeconds(60), result.ResendAvailableAtUtc);
        Assert.Empty(context.Sms.Messages);
        Assert.Single(context.OtpChallenges.Challenges);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        AuditRecordAssertions.AssertSingle(context.Audit, null, 11, "OtpResendCooldown", false);
    }

    [Fact]
    public async Task Challenge_can_be_replaced_at_the_exact_sixty_second_boundary()
    {
        // Break caught: extending the resend cooldown by treating its endpoint as unavailable.
        var context = new StartSignInContext();
        context.Clock.UtcNow = Now.AddSeconds(60);
        var priorChallenge = context.AddExistingChallenge(Now);

        var result = await context.UseCase.HandleAsync(context.Command, CancellationToken.None);

        Assert.Equal(StartSignInStatus.Succeeded, result.Status);
        Assert.Equal(OtpChallengeStatus.Superseded, priorChallenge.Status);
        Assert.Equal(2, context.OtpChallenges.Challenges.Count);
        Assert.Single(context.Sms.Messages);
    }

    private sealed class StartSignInContext
    {
        public StartSignInContext(bool addUser = true)
        {
            if (addUser)
            {
                User = Domain.Entities.User.Create(
                    "stable-user",
                    "DOMAIN\\user",
                    "Test",
                    "User",
                    "protected-mobile",
                    "masked-mobile",
                    Now.AddDays(-1));
                EntityId.Set(User, 11);
                User.SetLocalCredentials("LOCAL.USER", "password-hash", Now);
                Users.Users.Add(User);
            }

            Mobile.UnprotectedValues["protected-mobile"] = "synthetic-normalized-mobile";
            Hasher.Hashes[Tokens.SixDigitCode] = "A1B2";
            Passwords.Hashes["valid password"] = "password-hash";
            Tokens.AddOpaqueToken("challenge-token");
            UnitOfWork = new FakeUnitOfWork(OtpChallenges, Sessions);
            UseCase = new StartSignIn(
                Clock,
                Correlation,
                Users,
                OtpChallenges,
                Sms,
                Hasher,
                Passwords,
                Tokens,
                Mobile,
                Audit,
                UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now);

        public FakeCorrelationContext Correlation { get; } = new("trace-test");

        public FakeUserRepository Users { get; } = new();

        public FakeOtpChallengeRepository OtpChallenges { get; } = new();

        public FakeUserSessionRepository Sessions { get; } = new();

        public FakeSmsSender Sms { get; } = new();

        public FakeSecretHasher Hasher { get; } = new();

        public FakePasswordHasher Passwords { get; } = new();

        public FakeSecureTokenGenerator Tokens { get; } = new();

        public FakeMobileProtector Mobile { get; } = new();

        public FakeAuditWriter Audit { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; }

        public StartSignIn UseCase { get; }

        public User? User { get; }

        public StartSignInCommand Command { get; } = new(
            "local.user",
            "valid password",
            "network-bucket");

        public OtpChallenge AddExistingChallenge(DateTimeOffset createdAtUtc)
        {
            var challenge = OtpChallenge.Create(
                User!.Id,
                $"prior-{createdAtUtc.Ticks}",
                "C3D4",
                createdAtUtc,
                createdAtUtc.AddMinutes(5));
            challenge.MarkSent();
            EntityId.Set(challenge, 99);
            OtpChallenges.Challenges.Add(challenge);
            return challenge;
        }
    }
}
