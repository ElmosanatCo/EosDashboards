using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Entities;

public sealed class ExternalIdentityLink
{
    private ExternalIdentityLink()
    {
        NormalizedEmail = null!;
    }

    private ExternalIdentityLink(
        long userId,
        ExternalIdentityProvider provider,
        string normalizedEmail,
        DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        Provider = provider;
        NormalizedEmail = normalizedEmail;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public ExternalIdentityProvider Provider { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string? ProviderSubject { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LinkedAtUtc { get; private set; }

    public static ExternalIdentityLink CreatePending(
        long userId,
        ExternalIdentityProvider provider,
        string email,
        DateTimeOffset createdAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        return new ExternalIdentityLink(
            userId,
            provider,
            NormalizeEmail(email),
            createdAtUtc.ToUniversalTime());
    }

    public void BindSubject(string providerSubject, DateTimeOffset linkedAtUtc)
    {
        var normalizedSubject = NormalizeSubject(providerSubject);
        if (ProviderSubject is not null)
        {
            if (!string.Equals(ProviderSubject, normalizedSubject, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("An external identity link cannot change its provider subject.");
            }

            return;
        }

        ProviderSubject = normalizedSubject;
        LinkedAtUtc = linkedAtUtc.ToUniversalTime();
    }

    public void UpdateApprovedEmail(string email)
    {
        NormalizedEmail = NormalizeEmail(email);
    }

    public static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An external identity email is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        var atIndex = normalized.LastIndexOf('@');
        if (normalized.Length > 320 ||
            atIndex <= 0 ||
            atIndex == normalized.Length - 1)
        {
            throw new ArgumentException("An external identity email is invalid.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeSubject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An external identity subject is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return normalized;
    }
}
