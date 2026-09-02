using EosDashboards.Application.Auth;

namespace EosDashboards.Api.Auth;

public sealed record VerifyOtpRequest(string Code);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc,
    AuthenticatedUser User);

public sealed record ChallengeResponse(
    string ChallengeToken,
    string MaskedMobile,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ResendAvailableAtUtc);
