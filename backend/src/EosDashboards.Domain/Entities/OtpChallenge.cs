using System.Security.Cryptography;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Entities;

public sealed class OtpChallenge
{
    private const int MaximumFailedAttempts = 5;

    private OtpChallenge(
        long userId,
        string publicToken,
        string codeHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        PublicToken = publicToken;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ResendAvailableAtUtc = createdAtUtc.AddSeconds(60);
        Status = OtpChallengeStatus.Pending;
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public string PublicToken { get; private set; }

    public string CodeHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset ResendAvailableAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public int FailedAttemptCount { get; private set; }

    public OtpChallengeStatus Status { get; private set; }

    public static OtpChallenge Create(
        long userId,
        string publicToken,
        string codeHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(publicToken))
        {
            throw new ArgumentException("A public challenge token is required.", nameof(publicToken));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new ArgumentException("A code hash is required.", nameof(codeHash));
        }

        ValidateHash(codeHash, nameof(codeHash));
        var normalizedCreatedAtUtc = createdAtUtc.ToUniversalTime();
        var normalizedExpiresAtUtc = expiresAtUtc.ToUniversalTime();

        if (normalizedExpiresAtUtc - normalizedCreatedAtUtc != TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
        }

        return new OtpChallenge(
            userId,
            publicToken,
            codeHash,
            normalizedCreatedAtUtc,
            normalizedExpiresAtUtc);
    }

    public void MarkSent()
    {
        if (Status == OtpChallengeStatus.Pending)
        {
            Status = OtpChallengeStatus.Sent;
        }
    }

    public void MarkSendFailed()
    {
        if (Status is OtpChallengeStatus.Pending or OtpChallengeStatus.Sent)
        {
            Status = OtpChallengeStatus.SendFailed;
        }
    }

    public void Supersede()
    {
        if (Status is OtpChallengeStatus.Pending or OtpChallengeStatus.Sent)
        {
            Status = OtpChallengeStatus.Superseded;
        }
    }

    public bool Verify(string candidateHash, DateTimeOffset verifiedAtUtc)
    {
        if (Status != OtpChallengeStatus.Sent)
        {
            return false;
        }

        var normalizedVerifiedAtUtc = verifiedAtUtc.ToUniversalTime();
        if (normalizedVerifiedAtUtc >= ExpiresAtUtc)
        {
            Status = OtpChallengeStatus.Expired;
            return false;
        }

        bool hashesMatch;
        try
        {
            hashesMatch = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(CodeHash),
                Convert.FromHexString(candidateHash));
        }
        catch (FormatException)
        {
            return false;
        }

        if (hashesMatch)
        {
            Status = OtpChallengeStatus.Consumed;
            ConsumedAtUtc = normalizedVerifiedAtUtc;
            return true;
        }

        FailedAttemptCount++;
        if (FailedAttemptCount == MaximumFailedAttempts)
        {
            Status = OtpChallengeStatus.Exhausted;
        }

        return false;
    }

    private static void ValidateHash(string value, string parameterName)
    {
        try
        {
            _ = Convert.FromHexString(value);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The hash must be hexadecimal.", parameterName, exception);
        }
    }
}
