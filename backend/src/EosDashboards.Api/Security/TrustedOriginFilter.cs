using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace EosDashboards.Api.Security;

public sealed class TrustedOriginFilter(IOptions<ApiSecurityOptions> options)
{
    public bool IsTrusted(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (!options.Value.AllowedOrigins.Contains(origin, StringComparer.Ordinal))
        {
            return false;
        }

        var cookie = request.Cookies[RefreshCookieService.AntiforgeryCookieName];
        var header = request.Headers[RefreshCookieService.AntiforgeryHeaderName].ToString();
        if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(header))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cookie),
            Encoding.UTF8.GetBytes(header));
    }
}
