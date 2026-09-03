using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Security;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EosDashboards.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<NoStoreEndpointFilter>();

        group.MapPost("/sign-in/challenges", StartChallengeAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapGet("/providers", GetProviders);
        group.MapGet("/google/start", StartGoogleAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/sign-in/challenges/{challengeToken}/resend", ResendSignInAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/sign-in/challenges/{challengeToken}/verify", VerifyAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/password-reset/challenges", StartPasswordResetAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/password-reset/challenges/{challengeToken}/resend", ResendPasswordResetAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/password-reset/challenges/{challengeToken}/complete", CompletePasswordResetAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/password", ChangePasswordAsync)
            .RequireAuthorization("ActiveUser")
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/refresh", RefreshAsync)
            .RequireRateLimiting("auth-sensitive");
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization("ActiveUser");
        group.MapGet("/me", MeAsync)
            .RequireAuthorization("ActiveUser");

        return endpoints;
    }

    private static IResult GetProviders(IOptions<GoogleAuthenticationOptions> googleAuthentication) =>
        Results.Ok(new SignInProvidersResponse(googleAuthentication.Value.Enabled));

    private static IResult StartGoogleAsync(IOptions<GoogleAuthenticationOptions> googleAuthentication)
    {
        if (!googleAuthentication.Value.Enabled)
        {
            return Results.NotFound();
        }

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/EosDashboards/" },
            [GoogleAuthenticationOptions.Scheme]);
    }

    private static async Task<IResult> StartChallengeAsync(
        HttpContext context,
        SignInRequest request,
        StartSignIn startSignIn,
        CancellationToken cancellationToken)
    {
        var result = await startSignIn.HandleAsync(
            new StartSignInCommand(
                request.Username,
                request.Password,
                context.Connection.RemoteIpAddress?.ToString()),
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

    private static async Task<IResult> ResendSignInAsync(
        string challengeToken,
        HttpContext context,
        StartSignIn startSignIn,
        CancellationToken cancellationToken)
    {
        var result = await startSignIn.ResendAsync(
            new ResendOtpCommand(challengeToken, context.Connection.RemoteIpAddress?.ToString()),
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
            _ => ApiResults.Problem(context, 403, "otp_resend_denied", "The verification code is unavailable."),
        };
    }

    private static async Task<IResult> StartPasswordResetAsync(
        PasswordResetStartRequest request,
        HttpContext context,
        StartPasswordReset startPasswordReset,
        CancellationToken cancellationToken)
    {
        var result = await startPasswordReset.HandleAsync(
            new StartPasswordResetCommand(request.Username, context.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.Status switch
        {
            PasswordResetStartStatus.Succeeded => Results.Ok(new ChallengeResponse(
                result.ChallengeToken,
                "",
                result.ExpiresAtUtc,
                result.ResendAvailableAtUtc)),
            _ => ApiResults.Problem(
                context, 503, "sms_unavailable", "The verification service is temporarily unavailable."),
        };
    }

    private static async Task<IResult> ResendPasswordResetAsync(
        string challengeToken,
        HttpContext context,
        StartPasswordReset startPasswordReset,
        CancellationToken cancellationToken)
    {
        var result = await startPasswordReset.ResendAsync(
            new ResendOtpCommand(challengeToken, context.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.Status switch
        {
            PasswordResetStartStatus.Succeeded => Results.Ok(new ChallengeResponse(
                result.ChallengeToken,
                "",
                result.ExpiresAtUtc,
                result.ResendAvailableAtUtc)),
            _ => ApiResults.Problem(
                context, 503, "sms_unavailable", "The verification service is temporarily unavailable."),
        };
    }

    private static async Task<IResult> CompletePasswordResetAsync(
        string challengeToken,
        PasswordResetCompleteRequest request,
        HttpContext context,
        CompletePasswordReset completePasswordReset,
        CancellationToken cancellationToken)
    {
        var result = await completePasswordReset.HandleAsync(
            new CompletePasswordResetCommand(
                challengeToken,
                request.Code,
                request.NewPassword,
                context.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.Status is PasswordResetStatus.Succeeded
            ? Results.NoContent()
            : ApiResults.Problem(
                context, 401, "password_reset_failed", "The verification code is invalid or unavailable.");
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext context,
        ChangePassword changePassword,
        RefreshCookieService cookies,
        CancellationToken cancellationToken)
    {
        if (!SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId))
        {
            cookies.Expire(context.Response);
            return ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
        }

        var result = await changePassword.HandleAsync(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);
        if (result.Status is not ChangePasswordStatus.Succeeded)
        {
            return ApiResults.Problem(context, 400, "password_change_failed", "The password could not be changed.");
        }

        cookies.Expire(context.Response);
        return Results.NoContent();
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
        IRoleRepository roles,
        IDepartmentRepository departments,
        CancellationToken cancellationToken)
    {
        if (!SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId))
        {
            return ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.")
            : Results.Ok(await VerifyOtp.ProjectAsync(user, roles, departments, cancellationToken));
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
