using EosDashboards.Api.Security;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace EosDashboards.IntegrationTests.Security;

public sealed class GoogleAuthenticationCookieConfigurationTests
{
    [Fact]
    public void Correlation_and_nonce_cookies_allow_the_cross_site_oidc_callback()
    {
        var options = new OpenIdConnectOptions();

        GoogleAuthenticationCookieConfiguration.Configure(options);

        Assert.Equal(SameSiteMode.None, options.CorrelationCookie.SameSite);
        Assert.Equal(SameSiteMode.None, options.NonceCookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.CorrelationCookie.SecurePolicy);
        Assert.Equal(CookieSecurePolicy.Always, options.NonceCookie.SecurePolicy);
        Assert.Equal("/", options.CorrelationCookie.Path);
        Assert.Equal("/", options.NonceCookie.Path);
    }
}
