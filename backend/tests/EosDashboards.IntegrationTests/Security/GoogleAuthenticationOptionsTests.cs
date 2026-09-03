using EosDashboards.Api.Security;

namespace EosDashboards.IntegrationTests.Security;

public sealed class GoogleAuthenticationOptionsTests
{
    private readonly GoogleAuthenticationOptionsValidator _validator = new();

    [Fact]
    public void Disabled_google_does_not_require_client_settings()
    {
        var result = _validator.Validate(null, new GoogleAuthenticationOptions { Enabled = false });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Enabled_google_requires_complete_configuration_and_the_exact_https_callback()
    {
        var missingSecret = _validator.Validate(null, new GoogleAuthenticationOptions
        {
            Enabled = true,
            ClientId = "synthetic-client-id",
            RedirectUri = "https://localhost/EosDashboardsApi/api/v1/auth/google/callback",
        });
        var wrongCallback = _validator.Validate(null, new GoogleAuthenticationOptions
        {
            Enabled = true,
            ClientId = "synthetic-client-id",
            ClientSecret = "synthetic-client-secret",
            RedirectUri = "http://localhost/EosDashboardsApi/api/v1/auth/google/callback",
        });

        Assert.False(missingSecret.Succeeded);
        Assert.False(wrongCallback.Succeeded);
    }

    [Fact]
    public void Enabled_google_accepts_the_approved_local_callback()
    {
        var result = _validator.Validate(null, new GoogleAuthenticationOptions
        {
            Enabled = true,
            ClientId = "synthetic-client-id",
            ClientSecret = "synthetic-client-secret",
            RedirectUri = "https://localhost/EosDashboardsApi/api/v1/auth/google/callback",
        });

        Assert.True(result.Succeeded);
    }
}
