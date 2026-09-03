namespace EosDashboards.Api.Security;

public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "GoogleAuthentication";

    public const string Scheme = "Google";

    public const string CallbackPath = "/api/v1/auth/google/callback";

    public bool Enabled { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string? RedirectUri { get; init; }
}
