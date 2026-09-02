using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EosDashboards.IntegrationTests.Api;

public sealed class AuthEndpointTests : IClassFixture<AuthEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Fact]
    public async Task Liveness_is_available_without_database_or_authentication()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Refresh_rejects_an_untrusted_origin_and_expires_session_cookies()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", "https://untrusted.example");
        request.Headers.Add("Cookie", "__Host-Eos.Refresh=value; Eos.Antiforgery=token");
        request.Headers.Add("X-CSRF-TOKEN", "token");

        var response = await _client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("refresh_rejected", problem.GetProperty("code").GetString());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        Assert.Equal(2, values!.Count(value => value.Contains("expires=", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task OpenApi_exposes_the_approved_authentication_and_preference_routes()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/auth/challenges", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/refresh", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users/me/preferences", document, StringComparison.Ordinal);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _keyRingPath = Path.Combine(
            Path.GetTempPath(),
            $"eos-api-tests-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
            builder.UseSetting("ConnectionStrings:EosDashboard", "Server=(localdb)\\MSSQLLocalDB;Database=EosDashboards_Codex_IntegrationTests;Integrated Security=true;TrustServerCertificate=true");
            builder.UseSetting("ApiSecurity:AllowedOrigins:0", "https://localhost:5173");
            builder.UseSetting("AuthSecurity:HashingKey", key);
            builder.UseSetting("AuthSecurity:SigningKey", key);
            builder.UseSetting("AuthSecurity:Issuer", "EosDashboards.Tests");
            builder.UseSetting("AuthSecurity:Audience", "EosDashboards.Tests.Web");
            builder.UseSetting("AuthSecurity:AccessTokenLifetime", "00:10:00");
            builder.UseSetting("AuthSecurity:SessionLifetime", "08:00:00");
            builder.UseSetting("AuthSecurity:KeyRingPath", _keyRingPath);
            builder.UseSetting("Sms:Endpoint", "https://sms.test.invalid/soap");
            builder.UseSetting("Sms:Timeout", "00:00:01");
            builder.ConfigureServices(services => services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestWindowsHandler>(
                    NegotiateDefaults.AuthenticationScheme,
                    _ => { }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(_keyRingPath))
            {
                Directory.Delete(_keyRingPath, recursive: true);
            }
        }
    }

    private sealed class TestWindowsHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.PrimarySid, "S-1-5-21-test"),
                    new Claim(ClaimTypes.Name, "TEST\\user"),
                ],
                Scheme.Name,
                ClaimTypes.Name,
                ClaimTypes.Role);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
