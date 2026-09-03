using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Security;
using EosDashboards.Application.Preferences;

namespace EosDashboards.Api.Preferences;

public static class PreferenceEndpoints
{
    public static IEndpointRouteBuilder MapPreferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/users/me/preferences")
            .WithTags("Preferences")
            .RequireAuthorization("ActiveUser");
        group.MapGet("/", GetAsync);
        group.MapPut("/", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext context,
        GetMyPreferences getPreferences,
        CancellationToken cancellationToken)
    {
        if (!SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId))
        {
            return ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
        }

        return Results.Ok(await getPreferences.HandleAsync(userId, cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        HttpContext context,
        UpdateMyPreferencesCommand request,
        UpdateMyPreferences updatePreferences,
        CancellationToken cancellationToken)
    {
        if (!SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId))
        {
            return ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
        }

        return Results.Ok(await updatePreferences.HandleAsync(userId, request, cancellationToken));
    }
}
