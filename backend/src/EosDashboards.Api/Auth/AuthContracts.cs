using EosDashboards.Application.Auth;

namespace EosDashboards.Api.Auth;

public sealed record VerifyOtpRequest(string Code)
{
    public override string ToString() => nameof(VerifyOtpRequest);
}

public sealed record SignInRequest(string Username, string Password)
{
    public override string ToString() => nameof(SignInRequest);
}

public sealed record PasswordResetStartRequest(string Username)
{
    public override string ToString() => nameof(PasswordResetStartRequest);
}

public sealed record PasswordResetCompleteRequest(string Code, string NewPassword)
{
    public override string ToString() => nameof(PasswordResetCompleteRequest);
}

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword)
{
    public override string ToString() => nameof(ChangePasswordRequest);
}

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    DateTime SessionExpiresAt,
    AuthenticatedUser User);

public sealed record ChallengeResponse(
    string ChallengeToken,
    string MaskedMobile,
    DateTime ExpiresAt,
    DateTime ResendAvailableAt);

public sealed record SignInProvidersResponse(bool Google);
