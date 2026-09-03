using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Tests;

public sealed class OtpChallengeTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Create_preserves_local_millisecond_timestamp_names()
    {
        // Break caught: retaining UTC-normalized timestamp properties in an OTP challenge.
        var createdAt = new DateTime(2026, 9, 3, 18, 30, 15, 123, DateTimeKind.Unspecified);
        var challenge = OtpChallenge.Create(
            1,
            "local-token",
            "AABB",
            createdAt,
            createdAt.AddMinutes(5));

        Assert.Equal(createdAt, challenge.CreatedAt);
        Assert.Equal(createdAt.AddMinutes(5), challenge.ExpiresAt);
    }

    [Fact]
    public void Fifth_wrong_hash_exhausts_challenge()
    {
        // Break caught: allowing more than five incorrect verification attempts.
        var challenge = OtpChallenge.Create(1, "public-token", "AABB", Now, Now.AddMinutes(5));
        challenge.MarkSent();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            Assert.False(challenge.Verify("CCDD", Now.AddMinutes(1)));
        }

        Assert.Equal(OtpChallengeStatus.Exhausted, challenge.Status);
        Assert.Equal(5, challenge.FailedAttemptCount);
    }

    [Fact]
    public void Correct_hash_consumes_challenge_and_cannot_be_reused()
    {
        // Break caught: accepting a consumed OTP challenge more than once.
        var challenge = OtpChallenge.Create(1, "public-token", "AABB", Now, Now.AddMinutes(5));
        challenge.MarkSent();

        Assert.True(challenge.Verify("AABB", Now.AddMinutes(1)));
        Assert.False(challenge.Verify("AABB", Now.AddMinutes(1)));

        Assert.Equal(OtpChallengeStatus.Consumed, challenge.Status);
        Assert.Equal(Now.AddMinutes(1), challenge.ConsumedAt);
    }

    [Fact]
    public void Verification_at_exact_expiry_rejects_and_expires_challenge()
    {
        // Break caught: treating the expiry instant as still valid.
        var expiresAt = Now.AddMinutes(5);
        var challenge = OtpChallenge.Create(1, "public-token", "AABB", Now, expiresAt);
        challenge.MarkSent();

        Assert.False(challenge.Verify("AABB", expiresAt));

        Assert.Equal(OtpChallengeStatus.Expired, challenge.Status);
    }

    [Fact]
    public void Create_rejects_an_expiry_other_than_five_minutes()
    {
        // Break caught: issuing OTP challenges with a lifetime outside the approved five-minute window.
        Assert.Throws<ArgumentOutOfRangeException>(() => OtpChallenge.Create(
            1,
            "public-token",
            "AABB",
            Now,
            Now.AddMinutes(6)));
    }

    [Fact]
    public void Create_preserves_password_reset_purpose()
    {
        // Break caught: allowing reset OTPs to lose their purpose and be used as sign-in OTPs.
        var challenge = OtpChallenge.Create(
            1,
            "public-token",
            "AABB",
            Now,
            Now.AddMinutes(5),
            OtpChallengePurpose.PasswordReset);

        Assert.Equal(OtpChallengePurpose.PasswordReset, challenge.Purpose);
    }

    [Theory]
    [InlineData(ChallengeTerminalState.Superseded)]
    [InlineData(ChallengeTerminalState.SendFailed)]
    public void Non_sendable_challenge_rejects_verification(ChallengeTerminalState terminalState)
    {
        // Break caught: accepting OTPs after supersession or failed delivery.
        var challenge = OtpChallenge.Create(1, "public-token", "AABB", Now, Now.AddMinutes(5));

        if (terminalState == ChallengeTerminalState.Superseded)
        {
            challenge.Supersede();
        }
        else
        {
            challenge.MarkSendFailed();
        }

        Assert.False(challenge.Verify("AABB", Now.AddMinutes(1)));
    }

    [Fact]
    public void To_string_does_not_expose_otp_hash_or_code()
    {
        // Break caught: including authentication secrets in diagnostic text.
        var challenge = OtpChallenge.Create(1, "public-token", "AABBCCDDEEFF", Now, Now.AddMinutes(5));

        var text = challenge.ToString();

        Assert.DoesNotContain("AABBCCDDEEFF", text, StringComparison.Ordinal);
        Assert.DoesNotContain("123456", text, StringComparison.Ordinal);
    }

    public enum ChallengeTerminalState
    {
        Superseded,
        SendFailed,
    }
}
