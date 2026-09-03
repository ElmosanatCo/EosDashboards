namespace EosDashboards.Application.Auth;

public sealed record StartSignInCommand(string Username, string Password, string? NetworkKey)
{
    public override string ToString() => nameof(StartSignInCommand);
}

public sealed record VerifyOtpCommand(string ChallengeToken, string Code, string? NetworkKey)
{
    public override string ToString() => nameof(VerifyOtpCommand);
}

public sealed record StartPasswordResetCommand(string Username, string? NetworkKey)
{
    public override string ToString() => nameof(StartPasswordResetCommand);
}

public sealed record CompletePasswordResetCommand(
    string ChallengeToken,
    string Code,
    string NewPassword,
    string? NetworkKey)
{
    public override string ToString() => nameof(CompletePasswordResetCommand);
}

public sealed record ChangePasswordCommand(long UserId, string CurrentPassword, string NewPassword)
{
    public override string ToString() => nameof(ChangePasswordCommand);
}

public enum PasswordResetStatus
{
    Succeeded,
    Invalid,
}

public enum PasswordResetStartStatus
{
    Succeeded,
    DependencyUnavailable,
}

public sealed record PasswordResetStartResult(
    PasswordResetStartStatus Status,
    string ChallengeToken,
    DateTimeOffset ExpiresAtUtc)
{
    public override string ToString() => nameof(PasswordResetStartResult);
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

public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified)
{
    public override string ToString() => nameof(GoogleIdentity);
}

public enum GoogleSignInStatus
{
    Succeeded,
    Denied,
}

public sealed record GoogleSignInResult(
    GoogleSignInStatus Status,
    AuthenticationResult? Authentication)
{
    public override string ToString() => nameof(GoogleSignInResult);
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
