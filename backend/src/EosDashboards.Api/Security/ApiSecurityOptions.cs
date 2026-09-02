using Microsoft.Extensions.Options;

namespace EosDashboards.Api.Security;

public sealed class ApiSecurityOptions
{
    public const string SectionName = "ApiSecurity";

    public string[] AllowedOrigins { get; init; } = [];
}

internal sealed class ApiSecurityOptionsValidator : IValidateOptions<ApiSecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiSecurityOptions options)
    {
        if (options.AllowedOrigins.Length == 0 ||
            options.AllowedOrigins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment)))
        {
            return ValidateOptionsResult.Fail("ApiSecurityOptions.AllowedOrigins");
        }

        return ValidateOptionsResult.Success;
    }
}
