using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class GoogleSignInTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Verified_prelinked_email_binds_subject_and_issues_a_standard_session()
    {
        var context = new GoogleSignInContext();

        var result = await context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "person.synthetic@example.test", true),
            CancellationToken.None);

        Assert.Equal(GoogleSignInStatus.Succeeded, result.Status);
        var link = Assert.Single(context.Links.Links);
        Assert.Equal("google-subject-synthetic", link.ProviderSubject);
        Assert.Equal(context.Clock.UtcNow, link.LinkedAtUtc);
        var session = Assert.Single(context.Sessions.Sessions);
        Assert.Equal(context.User.Id, session.UserId);
        Assert.Equal(context.Clock.UtcNow.AddHours(8), session.ExpiresAtUtc);
        Assert.Equal("refresh-credential", result.Authentication?.RefreshCredential);
        Assert.Equal(context.Clock.UtcNow.AddMinutes(10), result.Authentication?.AccessToken?.ExpiresAtUtc);
        Assert.Equal(["GoogleSignIn"], context.UnitOfWork.OperationKeys);
        AuditRecordAssertions.AssertSingle(context.Audit, context.User.Id, context.User.Id, "GoogleAuthenticationSucceeded", true);
    }

    [Fact]
    public async Task Unverified_google_email_cannot_claim_a_pending_link()
    {
        var context = new GoogleSignInContext();

        var result = await context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "person.synthetic@example.test", false),
            CancellationToken.None);

        Assert.Equal(GoogleSignInStatus.Denied, result.Status);
        Assert.Null(Assert.Single(context.Links.Links).ProviderSubject);
        Assert.Empty(context.Sessions.Sessions);
        AuditRecordAssertions.AssertSingle(context.Audit, null, null, "GoogleAuthenticationDenied", false);
    }

    [Fact]
    public async Task Unknown_verified_google_email_cannot_create_a_session_or_a_link()
    {
        var context = new GoogleSignInContext();
        context.Links.Links.Clear();

        var result = await context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "unknown.synthetic@example.test", true),
            CancellationToken.None);

        Assert.Equal(GoogleSignInStatus.Denied, result.Status);
        Assert.Empty(context.Links.Links);
        Assert.Empty(context.Sessions.Sessions);
        AuditRecordAssertions.AssertSingle(context.Audit, null, null, "GoogleAuthenticationDenied", false);
    }

    [Fact]
    public async Task Existing_google_subject_remains_authoritative_after_an_explicit_email_update()
    {
        var context = new GoogleSignInContext();
        var link = Assert.Single(context.Links.Links);
        link.BindSubject("google-subject-synthetic", Now.AddMinutes(-1));
        link.UpdateApprovedEmail("updated.synthetic@example.test");

        var result = await context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "different.synthetic@example.test", true),
            CancellationToken.None);

        Assert.Equal(GoogleSignInStatus.Succeeded, result.Status);
        Assert.Single(context.Sessions.Sessions);
        Assert.Equal("google-subject-synthetic", link.ProviderSubject);
    }

    [Fact]
    public async Task Inactive_user_cannot_sign_in_with_a_linked_google_subject()
    {
        var context = new GoogleSignInContext();
        var link = Assert.Single(context.Links.Links);
        link.BindSubject("google-subject-synthetic", Now.AddMinutes(-1));
        context.User.Deactivate(Now.AddMinutes(-1));

        var result = await context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "person.synthetic@example.test", true),
            CancellationToken.None);

        Assert.Equal(GoogleSignInStatus.Denied, result.Status);
        Assert.Empty(context.Sessions.Sessions);
        AuditRecordAssertions.AssertSingle(context.Audit, null, context.User.Id, "GoogleAuthenticationDenied", false);
    }

    [Fact]
    public async Task Audit_failure_after_google_success_still_saves_the_link_and_session_before_rethrowing()
    {
        var context = new GoogleSignInContext();
        context.Audit.Exception = new InvalidOperationException("synthetic audit failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.UseCase.HandleAsync(
            new GoogleIdentity("google-subject-synthetic", "person.synthetic@example.test", true),
            CancellationToken.None));

        Assert.Equal("google-subject-synthetic", Assert.Single(context.Links.Links).ProviderSubject);
        Assert.Single(context.Sessions.Sessions);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    private sealed class GoogleSignInContext
    {
        public GoogleSignInContext()
        {
            User = User.Create(
                "synthetic-google-user",
                "DOMAIN\\synthetic.google",
                "Synthetic",
                "Google",
                "protected-mobile",
                "*******6789",
                Now.AddDays(-1));
            EntityId.Set(User, 11);
            User.AssignRole(31);
            Users.Users.Add(User);
            Links.Links.Add(ExternalIdentityLink.CreatePending(
                User.Id,
                ExternalIdentityProvider.Google,
                "person.synthetic@example.test",
                Now.AddDays(-1)));
            Hasher.Hashes["refresh-credential"] = "refresh-hash";
            Tokens.AddOpaqueToken("refresh-credential");
            UnitOfWork = new FakeUnitOfWork(OtpChallenges, Sessions);
            UseCase = new GoogleSignIn(
                Clock,
                Correlation,
                Users,
                Links,
                Sessions,
                Hasher,
                Tokens,
                TokenIssuer,
                Audit,
                UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now);

        public FakeCorrelationContext Correlation { get; } = new("trace-test");

        public FakeUserRepository Users { get; } = new();

        public FakeExternalIdentityLinkRepository Links { get; } = new();

        public FakeOtpChallengeRepository OtpChallenges { get; } = new();

        public FakeUserSessionRepository Sessions { get; } = new();

        public FakeSecretHasher Hasher { get; } = new();

        public FakeSecureTokenGenerator Tokens { get; } = new();

        public FakeAccessTokenIssuer TokenIssuer { get; } = new();

        public FakeAuditWriter Audit { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; }

        public GoogleSignIn UseCase { get; }

        public User User { get; }
    }
}
