using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Security;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace EosDashboards.Api.Auth;

public static class GoogleAuthenticationEvents
{
    public static OpenIdConnectEvents Create() => new()
    {
        OnTokenValidated = CompleteSignInAsync,
        OnRemoteFailure = RedirectToSignInAsync,
    };

    private static async Task CompleteSignInAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = context.Principal?.FindFirst("email")?.Value;
        var verified = bool.TryParse(
            context.Principal?.FindFirst("email_verified")?.Value,
            out var emailVerified) && emailVerified;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            await WriteFailureAuditAsync(context.HttpContext);
            await RedirectToSignInAsync(context);
            return;
        }

        try
        {
            var signIn = context.HttpContext.RequestServices.GetRequiredService<GoogleSignIn>();
            var result = await signIn.HandleAsync(
                new GoogleIdentity(subject, email, verified),
                context.HttpContext.RequestAborted);
            if (result.Status != GoogleSignInStatus.Succeeded)
            {
                await RedirectToSignInAsync(context);
                return;
            }

            var authentication = result.Authentication!;
            context.HttpContext.RequestServices
                .GetRequiredService<RefreshCookieService>()
                .Set(context.Response, authentication.RefreshCredential!, authentication.SessionExpiresAt!.Value);
            context.Response.Redirect("/EosDashboards/");
            context.HandleResponse();
        }
        catch
        {
            await WriteFailureAuditAsync(context.HttpContext);
            await RedirectToSignInAsync(context);
        }
    }

    private static async Task RedirectToSignInAsync(RemoteFailureContext context)
    {
        await WriteFailureAuditAsync(context.HttpContext);
        context.Response.Redirect("/EosDashboards/?authError=google");
        context.HandleResponse();
    }

    private static Task RedirectToSignInAsync(TokenValidatedContext context)
    {
        context.Response.Redirect("/EosDashboards/?authError=google");
        context.HandleResponse();
        return Task.CompletedTask;
    }

    private static async Task WriteFailureAuditAsync(HttpContext context)
    {
        try
        {
            await context.RequestServices.GetRequiredService<IAuditWriter>().WriteAsync(
                new AuditRecord(null, null, "GoogleAuthenticationFailed", false, context.TraceIdentifier, null),
                CancellationToken.None);
            await context.RequestServices.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);
        }
        catch
        {
            // The visitor must still receive the generic safe redirect if observability is unavailable.
        }
    }
}
