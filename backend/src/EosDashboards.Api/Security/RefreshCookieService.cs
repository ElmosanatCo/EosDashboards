using EosDashboards.Application.Abstractions;

namespace EosDashboards.Api.Security;

public sealed class RefreshCookieService(ISecureTokenGenerator tokens)
{
    public const string RefreshCookieName = "__Host-Eos.Refresh";
    public const string AntiforgeryCookieName = "Eos.Antiforgery";
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";

    public string Set(HttpResponse response, string refreshCredential, DateTime expiresAt)
    {
        var antiforgeryToken = tokens.CreateOpaqueToken(32);
        response.Cookies.Append(
            RefreshCookieName,
            refreshCredential,
            CookieOptions(expiresAt, httpOnly: true));
        response.Cookies.Append(
            AntiforgeryCookieName,
            antiforgeryToken,
            CookieOptions(expiresAt, httpOnly: false));
        return antiforgeryToken;
    }

    public void Expire(HttpResponse response)
    {
        response.Cookies.Delete(RefreshCookieName, DeleteOptions(httpOnly: true));
        response.Cookies.Delete(AntiforgeryCookieName, DeleteOptions(httpOnly: false));
    }

    private static CookieOptions CookieOptions(DateTime expiresAt, bool httpOnly) => new()
    {
        Expires = expiresAt,
        HttpOnly = httpOnly,
        IsEssential = true,
        Path = "/",
        SameSite = SameSiteMode.Strict,
        Secure = true,
    };

    private static CookieOptions DeleteOptions(bool httpOnly) => new()
    {
        HttpOnly = httpOnly,
        IsEssential = true,
        Path = "/",
        SameSite = SameSiteMode.Strict,
        Secure = true,
    };
}
