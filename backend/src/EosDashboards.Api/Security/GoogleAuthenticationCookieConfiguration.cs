using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace EosDashboards.Api.Security;

public static class GoogleAuthenticationCookieConfiguration
{
    public static void Configure(OpenIdConnectOptions options)
    {
        options.CorrelationCookie.Name = "__Host-Eos.Google.Correlation";
        options.CorrelationCookie.Path = "/";
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.Name = "__Host-Eos.Google.Nonce";
        options.NonceCookie.Path = "/";
        options.NonceCookie.HttpOnly = true;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
    }
}
