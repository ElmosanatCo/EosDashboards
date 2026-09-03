using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

public sealed class ChangePasswordTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void Password_policy_rejects_values_shorter_than_eight_characters(string password)
    {
        // Break caught: accepting a password below the approved minimum length.
        Assert.False(PasswordPolicy.IsValid(password));
    }

    [Fact]
    public void Password_policy_accepts_an_eight_character_password_without_composition_rule()
    {
        // Break caught: imposing an unapproved composition policy on a valid password.
        Assert.True(PasswordPolicy.IsValid("aaaaaaaa"));
    }

    [Fact]
    public async Task Change_password_replaces_the_hash_and_revokes_active_sessions()
    {
        // Break caught: retaining a valid session after a credential change.
        var user = User.Create("stable", "account", "Test", "User", "protected-mobile", "masked-mobile", 1, Now);
        EntityId.Set(user, 11);
        user.SetLocalCredentials("LOCAL.USER", "old-hash", Now);
        var users = new FakeUserRepository();
        users.Users.Add(user);
        var sessions = new FakeUserSessionRepository();
        var session = UserSession.Create(user.Id, "refresh-hash", Now);
        EntityId.Set(session, 201);
        sessions.Sessions.Add(session);
        var passwords = new FakePasswordHasher();
        passwords.Hashes["old password"] = "old-hash";
        passwords.Hashes["new password"] = "new-hash";
        var challenges = new FakeOtpChallengeRepository();
        var unitOfWork = new FakeUnitOfWork(challenges, sessions);
        var change = new ChangePassword(
            new FakeClock(Now.AddMinutes(1)),
            new FakeCorrelationContext("trace-test"),
            users,
            sessions,
            passwords,
            new FakeAuditWriter(),
            unitOfWork);

        var result = await change.HandleAsync(
            new ChangePasswordCommand(user.Id, "old password", "new password"),
            CancellationToken.None);

        Assert.Equal(ChangePasswordStatus.Succeeded, result.Status);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(SessionRevocationReason.PasswordChanged, session.RevocationReason);
    }

    private static readonly DateTime Now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);
}
