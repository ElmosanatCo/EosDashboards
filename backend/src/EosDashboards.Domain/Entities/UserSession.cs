using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Entities;

public sealed class UserSession
{
    private static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(8);

    private UserSession(long userId, string refreshCredentialHash, DateTime createdAt)
    {
        UserId = userId;
        RefreshCredentialHash = refreshCredentialHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(AbsoluteLifetime);
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public string RefreshCredentialHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? LastRefreshedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public SessionRevocationReason? RevocationReason { get; private set; }

    public static UserSession Create(long userId, string refreshCredentialHash, DateTime createdAt)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(refreshCredentialHash))
        {
            throw new ArgumentException("A refresh credential hash is required.", nameof(refreshCredentialHash));
        }

        return new UserSession(userId, refreshCredentialHash, createdAt);
    }

    public void Rotate(string replacementRefreshCredentialHash, DateTime rotatedAt)
    {
        if (string.IsNullOrWhiteSpace(replacementRefreshCredentialHash))
        {
            throw new ArgumentException("A refresh credential hash is required.", nameof(replacementRefreshCredentialHash));
        }

        if (string.Equals(
                replacementRefreshCredentialHash,
                RefreshCredentialHash,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The replacement refresh credential hash must differ from the current hash.",
                nameof(replacementRefreshCredentialHash));
        }

        var normalizedRotatedAt = rotatedAt;
        if (!IsActive(normalizedRotatedAt))
        {
            return;
        }

        RefreshCredentialHash = replacementRefreshCredentialHash;
        LastRefreshedAt = normalizedRotatedAt;
    }

    public void Revoke(SessionRevocationReason reason, DateTime revokedAt)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = revokedAt;
        RevocationReason = reason;
    }

    public bool IsActive(DateTime at)
    {
        return !RevokedAt.HasValue &&
               CreatedAt <= at &&
               at < ExpiresAt;
    }
}
