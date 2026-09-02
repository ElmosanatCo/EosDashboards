namespace EosDashboards.Application.Auth;

public sealed record OrganizationalIdentity(string StableId, string AccountName)
{
    public override string ToString() => nameof(OrganizationalIdentity);
}

public sealed record StartSignInCommand(OrganizationalIdentity Identity, string? NetworkKey)
{
    public override string ToString() => nameof(StartSignInCommand);
}

public sealed record VerifyOtpCommand(string ChallengeToken, string Code, string? NetworkKey)
{
    public override string ToString() => nameof(VerifyOtpCommand);
}

public sealed record RefreshSessionCommand(string RefreshCredential)
{
    public override string ToString() => nameof(RefreshSessionCommand);
}

public sealed record LogoutCommand(long SessionId);

public sealed record SmsMessage(string Mobile, string Text)
{
    public override string ToString() => nameof(SmsMessage);
}

public sealed record SmsSendResult(bool Succeeded, string? SafeErrorCode);

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    public override string ToString() => nameof(IssuedAccessToken);
}

public enum StartSignInStatus
{
    Succeeded,
    Denied,
    Cooldown,
    DependencyUnavailable,
}

public sealed record StartSignInResult(
    StartSignInStatus Status,
    string? ChallengeToken,
    string? MaskedMobile,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ResendAvailableAtUtc)
{
    public override string ToString() => nameof(StartSignInResult);
}

public enum VerifyOtpStatus
{
    Succeeded,
    Invalid,
    Expired,
    Exhausted,
    Consumed,
}

public sealed record AuthenticatedUser(
    long Id,
    string AccountName,
    string FirstName,
    string LastName,
    IReadOnlyCollection<long> RoleIds)
{
    public override string ToString() => nameof(AuthenticatedUser);
}

public sealed record AuthenticationResult(
    VerifyOtpStatus Status,
    IssuedAccessToken? AccessToken,
    string? RefreshCredential,
    DateTimeOffset? SessionExpiresAtUtc,
    AuthenticatedUser? User)
{
    public override string ToString() => nameof(AuthenticationResult);
}

public enum RefreshSessionStatus
{
    Succeeded,
    Denied,
}

public sealed record RefreshSessionResult(
    RefreshSessionStatus Status,
    IssuedAccessToken? AccessToken,
    string? RefreshCredential,
    DateTimeOffset? SessionExpiresAtUtc,
    AuthenticatedUser? User)
{
    public override string ToString() => nameof(RefreshSessionResult);
}
