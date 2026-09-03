using Microsoft.Extensions.Options;

namespace EosDashboards.Api.Security;

public sealed class GoogleAuthenticationOptionsValidator : IValidateOptions<GoogleAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, GoogleAuthenticationOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret) ||
            string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            return ValidateOptionsResult.Fail("Google sign-in configuration is incomplete.");
        }

        if (!Uri.TryCreate(options.RedirectUri, UriKind.Absolute, out var redirectUri) ||
            !string.Equals(redirectUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(redirectUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            redirectUri.Port != 443 ||
            !string.Equals(redirectUri.AbsolutePath, "/EosDashboardsApi" + GoogleAuthenticationOptions.CallbackPath, StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(redirectUri.Query) ||
            !string.IsNullOrEmpty(redirectUri.Fragment))
        {
            return ValidateOptionsResult.Fail("Google sign-in callback configuration is invalid.");
        }

        return ValidateOptionsResult.Success;
    }
}
