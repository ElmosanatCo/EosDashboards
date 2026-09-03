using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class VerifyOtpTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task Correct_otp_consumes_challenge_then_atomically_creates_an_eight_hour_session()
    {
        // Break caught: creating credentials before OTP consumption or saving consumption and session separately.
        var context = new VerifyOtpContext();

        var result = await context.UseCase.HandleAsync(context.Command(), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Succeeded, result.Status);
        Assert.Equal(OtpChallengeStatus.Consumed, context.Challenge.Status);
        Assert.Equal([OtpChallengeStatus.Consumed], context.Sessions.ChallengeStatusesAtAdd);
        var session = Assert.Single(context.Sessions.Sessions);
        Assert.Equal("E5F6", session.RefreshCredentialHash);
        Assert.Equal(context.Clock.Now.AddHours(8), session.ExpiresAt);
        Assert.Equal("refresh-credential", result.RefreshCredential);
        Assert.Equal(context.Clock.Now.AddHours(8), result.SessionExpiresAt);
        Assert.Equal(context.Clock.Now.AddMinutes(10), result.AccessToken?.ExpiresAt);
        Assert.Equal(context.User.Id, result.User?.Id);
        Assert.False(result.User?.MustChangePassword);
        Assert.Equal([31], result.User?.RoleIds);
        Assert.Equal(["SystemAdministrator"], result.User?.RoleCodes);
        Assert.Equal(1, result.User?.Department.Id);
        Assert.Equal("واحد آزمایشی", result.User?.Department.Name);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        var save = Assert.Single(context.UnitOfWork.Observations);
        Assert.Equal(OtpChallengeStatus.Consumed, Assert.Single(save.ChallengeStatuses));
        Assert.Equal(1, save.SessionCount);
        Assert.Equal(context.Clock.Now.AddMinutes(10), Assert.Single(context.TokenIssuer.Requests).ExpiresAt);
        AuditRecordAssertions.AssertSingle(context.Audit, 11, 11, "AuthenticationSucceeded", true);
        Assert.DoesNotContain("refresh-credential", session.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Wrong_otp_persists_one_attempt_without_creating_a_session()
    {
        // Break caught: failing to persist an invalid attempt or creating a session on a wrong OTP.
        var context = new VerifyOtpContext();

        var result = await context.UseCase.HandleAsync(context.Command("000000"), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Invalid, result.Status);
        Assert.Equal(1, context.Challenge.FailedAttemptCount);
        Assert.Empty(context.Sessions.Sessions);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshCredential);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        AuditRecordAssertions.AssertSingle(context.Audit, null, 11, "OtpVerificationFailed", false);
    }

    [Fact]
    public async Task Successful_otp_projects_the_temporary_password_requirement()
    {
        var context = new VerifyOtpContext();
        context.User.SetTemporaryLocalCredentials("LOCAL.USER", "temporary-hash", Now);

        var result = await context.UseCase.HandleAsync(context.Command(), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Succeeded, result.Status);
        Assert.True(result.User?.MustChangePassword);
    }

    [Fact]
    public async Task Otp_at_the_exact_expiry_is_rejected_and_persisted_as_expired()
    {
        // Break caught: treating the five-minute endpoint as a valid verification instant.
        var context = new VerifyOtpContext();
        context.Clock.Now = Now.AddMinutes(5);

        var result = await context.UseCase.HandleAsync(context.Command(), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Expired, result.Status);
        Assert.Equal(OtpChallengeStatus.Expired, context.Challenge.Status);
        Assert.Empty(context.Sessions.Sessions);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Fifth_wrong_otp_exhausts_the_challenge_without_creating_a_session()
    {
        // Break caught: allowing a sixth OTP attempt through the application boundary.
        var context = new VerifyOtpContext();
        AuthenticationResult? result = null;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            result = await context.UseCase.HandleAsync(context.Command("000000"), CancellationToken.None);
        }

        Assert.Equal(VerifyOtpStatus.Exhausted, result?.Status);
        Assert.Equal(OtpChallengeStatus.Exhausted, context.Challenge.Status);
        Assert.Equal(5, context.Challenge.FailedAttemptCount);
        Assert.Empty(context.Sessions.Sessions);
        Assert.Equal(5, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Consumed_challenge_cannot_create_another_session()
    {
        // Break caught: reusing a successfully consumed challenge token.
        var context = new VerifyOtpContext();
        Assert.True(context.Challenge.Verify("A1B2", Now.AddMinutes(-1)));

        var result = await context.UseCase.HandleAsync(context.Command(), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Consumed, result.Status);
        Assert.Empty(context.Sessions.Sessions);
        Assert.Empty(context.TokenIssuer.Requests);
    }

    [Fact]
    public async Task Password_reset_challenge_cannot_create_a_sign_in_session()
    {
        // Break caught: accepting an OTP issued for recovery as proof of a completed sign-in.
        var context = new VerifyOtpContext(OtpChallengePurpose.PasswordReset);

        var result = await context.UseCase.HandleAsync(context.Command(), CancellationToken.None);

        Assert.Equal(VerifyOtpStatus.Invalid, result.Status);
        Assert.Empty(context.Sessions.Sessions);
    }

    [Fact]
    public async Task Cancellation_during_verification_cannot_evade_persisting_a_failed_attempt()
    {
        // Break caught: honoring a client abort after OTP mutation and losing the security attempt counter.
        var context = new VerifyOtpContext();
        using var cancellation = new CancellationTokenSource();
        context.Hasher.OnHash = _ => cancellation.Cancel();

        var result = await context.UseCase.HandleAsync(context.Command("000000"), cancellation.Token);

        Assert.Equal(VerifyOtpStatus.Invalid, result.Status);
        Assert.Equal(1, context.Challenge.FailedAttemptCount);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.Equal(CancellationToken.None, Assert.Single(context.Audit.CancellationTokens));
        Assert.Equal(CancellationToken.None, Assert.Single(context.UnitOfWork.CancellationTokens));
        Assert.Equal(cancellation.Token, Assert.Single(context.OtpChallenges.FindTokens));
        Assert.Equal(cancellation.Token, Assert.Single(context.Users.GetTokens));
    }

    [Fact]
    public async Task Audit_failure_after_wrong_otp_still_saves_the_failed_attempt_before_rethrowing()
    {
        // Break caught: audit staging failure rolling back a security-relevant failed attempt.
        var context = new VerifyOtpContext();
        context.Audit.Exception = new InvalidOperationException("synthetic audit failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.HandleAsync(context.Command("000000"), CancellationToken.None));

        Assert.Equal(1, context.Challenge.FailedAttemptCount);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.Equal(CancellationToken.None, Assert.Single(context.Audit.CancellationTokens));
        Assert.Equal(CancellationToken.None, Assert.Single(context.UnitOfWork.CancellationTokens));
        Assert.Empty(context.Sessions.Sessions);
    }

    [Fact]
    public async Task Audit_failure_after_success_still_atomically_saves_consumption_and_session_before_rethrowing()
    {
        // Break caught: audit staging failure discarding consumed OTP and its associated session state.
        var context = new VerifyOtpContext();
        context.Audit.Exception = new InvalidOperationException("synthetic audit failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.HandleAsync(context.Command(), CancellationToken.None));

        Assert.Equal(OtpChallengeStatus.Consumed, context.Challenge.Status);
        Assert.Single(context.Sessions.Sessions);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        var save = Assert.Single(context.UnitOfWork.Observations);
        Assert.Equal(OtpChallengeStatus.Consumed, Assert.Single(save.ChallengeStatuses));
        Assert.Equal(1, save.SessionCount);
        Assert.Equal(CancellationToken.None, Assert.Single(context.Audit.CancellationTokens));
        Assert.Equal(CancellationToken.None, Assert.Single(context.UnitOfWork.CancellationTokens));
    }

    private sealed class VerifyOtpContext
    {
        public VerifyOtpContext(OtpChallengePurpose purpose = OtpChallengePurpose.SignIn)
        {
            User = DomainUser();
            Users.Users.Add(User);
            Challenge = OtpChallenge.Create(
                User.Id,
                "challenge-token",
                "A1B2",
                Now,
                Now.AddMinutes(5),
                purpose);
            Challenge.MarkSent();
            EntityId.Set(Challenge, 101);
            OtpChallenges.Challenges.Add(Challenge);
            Hasher.Hashes["246810"] = "A1B2";
            Hasher.Hashes["000000"] = "C3D4";
            Hasher.Hashes["refresh-credential"] = "E5F6";
            Tokens.AddOpaqueToken("refresh-credential");
            Sessions.CurrentChallengeStatus = () => Challenge.Status;
            UnitOfWork = new FakeUnitOfWork(OtpChallenges, Sessions);
            UseCase = new VerifyOtp(
                Clock,
                Correlation,
                Users,
                Roles,
                Departments,
                OtpChallenges,
                Sessions,
                Hasher,
                Tokens,
                TokenIssuer,
                Audit,
                UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now.AddMinutes(1));

        public FakeCorrelationContext Correlation { get; } = new("trace-test");

        public FakeUserRepository Users { get; } = new();

        public FakeRoleRepository Roles { get; } = CreateRoles();

        public FakeDepartmentRepository Departments { get; } = CreateDepartments();

        public FakeOtpChallengeRepository OtpChallenges { get; } = new();

        public FakeUserSessionRepository Sessions { get; } = new();

        public FakeSecretHasher Hasher { get; } = new();

        public FakeSecureTokenGenerator Tokens { get; } = new();

        public FakeAccessTokenIssuer TokenIssuer { get; } = new();

        public FakeAuditWriter Audit { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; }

        private static FakeRoleRepository CreateRoles()
        {
            var repository = new FakeRoleRepository();
            var role = Role.Create("SystemAdministrator", "مدیر سامانه", true, Now);
            EntityId.Set(role, 31);
            repository.Roles.Add(role);
            return repository;
        }

        private static FakeDepartmentRepository CreateDepartments()
        {
            var repository = new FakeDepartmentRepository();
            var department = Department.CreateRoot("واحد آزمایشی", Now);
            EntityId.Set(department, 1);
            repository.Departments.Add(department);
            return repository;
        }

        public VerifyOtp UseCase { get; }

        public User User { get; }

        public OtpChallenge Challenge { get; }

        public VerifyOtpCommand Command(string code = "246810") => new("challenge-token", code, "network-bucket");

        private static User DomainUser()
        {
            var user = User.Create(
                "stable-user",
                "DOMAIN\\user",
                "Test",
                "User",
                "protected-mobile",
                "masked-mobile",
                1,
                Now.AddDays(-1));
            EntityId.Set(user, 11);
            user.AssignRole(31);
            return user;
        }
    }
}
