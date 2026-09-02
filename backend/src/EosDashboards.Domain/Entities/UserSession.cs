using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Entities;

public sealed class UserSession
{
    private static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(8);

    private UserSession(long userId, string refreshCredentialHash, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        RefreshCredentialHash = refreshCredentialHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = createdAtUtc.Add(AbsoluteLifetime);
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public string RefreshCredentialHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? LastRefreshedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public SessionRevocationReason? RevocationReason { get; private set; }

    public static UserSession Create(long userId, string refreshCredentialHash, DateTimeOffset createdAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(refreshCredentialHash))
        {
            throw new ArgumentException("A refresh credential hash is required.", nameof(refreshCredentialHash));
        }

        return new UserSession(userId, refreshCredentialHash, createdAtUtc.ToUniversalTime());
    }

    public void Rotate(string replacementRefreshCredentialHash, DateTimeOffset rotatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(replacementRefreshCredentialHash))
        {
            throw new ArgumentException("A refresh credential hash is required.", nameof(replacementRefreshCredentialHash));
        }

        var normalizedRotatedAtUtc = rotatedAtUtc.ToUniversalTime();
        if (!IsActive(normalizedRotatedAtUtc))
        {
            return;
        }

        RefreshCredentialHash = replacementRefreshCredentialHash;
        LastRefreshedAtUtc = normalizedRotatedAtUtc;
    }

    public void Revoke(SessionRevocationReason reason, DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc.ToUniversalTime();
        RevocationReason = reason;
    }

    public bool IsActive(DateTimeOffset atUtc)
    {
        return !RevokedAtUtc.HasValue && atUtc.ToUniversalTime() < ExpiresAtUtc;
    }
}
