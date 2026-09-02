using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Security;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;

namespace EosDashboards.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<NoStoreEndpointFilter>();

        group.MapPost("/challenges", StartChallengeAsync)
            .RequireAuthorization("WindowsIdentity")
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/challenges/{challengeToken}/verify", VerifyAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/refresh", RefreshAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization("ActiveUser");
        group.MapGet("/me", MeAsync)
            .RequireAuthorization("ActiveUser");

        return endpoints;
    }

    private static async Task<IResult> StartChallengeAsync(
        HttpContext context,
        IWindowsIdentityReader identityReader,
        StartSignIn startSignIn,
        CancellationToken cancellationToken)
    {
        var identity = identityReader.Read(context.User);
        if (identity is null)
        {
            return ApiResults.Problem(context, 401, "windows_identity_unavailable", "Organizational sign-in is required.");
        }

        var result = await startSignIn.HandleAsync(
            new StartSignInCommand(identity, context.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.Status switch
        {
            StartSignInStatus.Succeeded => Results.Ok(new ChallengeResponse(
                result.ChallengeToken!,
                result.MaskedMobile!,
                result.ExpiresAtUtc!.Value,
                result.ResendAvailableAtUtc!.Value)),
            StartSignInStatus.Cooldown => Results.Json(
                new { code = "otp_cooldown", retryAtUtc = result.ResendAvailableAtUtc, traceId = context.TraceIdentifier },
                statusCode: StatusCodes.Status429TooManyRequests),
            StartSignInStatus.DependencyUnavailable => ApiResults.Problem(
                context, 503, "sms_unavailable", "The verification service is temporarily unavailable."),
            _ => ApiResults.Problem(context, 403, "sign_in_denied", "Sign-in is not available for this account."),
        };
    }

    private static async Task<IResult> VerifyAsync(
        string challengeToken,
        VerifyOtpRequest request,
        HttpContext context,
        VerifyOtp verifyOtp,
        RefreshCookieService cookies,
        CancellationToken cancellationToken)
    {
        var result = await verifyOtp.HandleAsync(
            new VerifyOtpCommand(
                challengeToken,
                request.Code,
                context.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        if (result.Status != VerifyOtpStatus.Succeeded)
        {
            cookies.Expire(context.Response);
            return ApiResults.Problem(context, 401, "otp_verification_failed", "The verification code is invalid or unavailable.");
        }

        cookies.Set(context.Response, result.RefreshCredential!, result.SessionExpiresAtUtc!.Value);
        return Results.Ok(new AuthResponse(
            result.AccessToken!.Value,
            result.AccessToken.ExpiresAtUtc,
            result.SessionExpiresAtUtc.Value,
            result.User!));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext context,
        TrustedOriginFilter trustedOrigin,
        RefreshSession refreshSession,
        RefreshCookieService cookies,
        CancellationToken cancellationToken)
    {
        if (!trustedOrigin.IsTrusted(context.Request) ||
            !context.Request.Cookies.TryGetValue(RefreshCookieService.RefreshCookieName, out var refreshCredential))
        {
            cookies.Expire(context.Response);
            return ApiResults.Problem(context, 403, "refresh_rejected", "The session could not be refreshed.");
        }

        var result = await refreshSession.HandleAsync(
            new RefreshSessionCommand(refreshCredential),
            cancellationToken);
        if (result.Status != RefreshSessionStatus.Succeeded)
        {
            cookies.Expire(context.Response);
            return ApiResults.Problem(context, 401, "refresh_denied", "The session could not be refreshed.");
        }

        cookies.Set(context.Response, result.RefreshCredential!, result.SessionExpiresAtUtc!.Value);
        return Results.Ok(new AuthResponse(
            result.AccessToken!.Value,
            result.AccessToken.ExpiresAtUtc,
            result.SessionExpiresAtUtc.Value,
            result.User!));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        TrustedOriginFilter trustedOrigin,
        Logout logout,
        RefreshCookieService cookies,
        CancellationToken cancellationToken)
    {
        if (!trustedOrigin.IsTrusted(context.Request) ||
            !SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sid, out var sessionId))
        {
            cookies.Expire(context.Response);
            return ApiResults.Problem(context, 403, "logout_rejected", "The session could not be closed.");
        }

        await logout.HandleAsync(new LogoutCommand(sessionId), cancellationToken);
        cookies.Expire(context.Response);
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        HttpContext context,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        if (!SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId))
        {
            return ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.")
            : Results.Ok(VerifyOtp.Project(user));
    }
}

public sealed class NoStoreEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
        return await next(context);
    }
}
