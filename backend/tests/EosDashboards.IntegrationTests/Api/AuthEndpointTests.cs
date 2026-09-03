using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using EosDashboards.Api.Auth;
using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

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
    public async Task OpenApi_exposes_local_credential_routes_without_the_windows_route()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/auth/sign-in/challenges", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/providers", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/google/start", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/password-reset/challenges", document, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v1/auth/challenges\"", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/refresh", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users/me/preferences", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Providers_reports_google_as_unavailable_when_it_is_disabled()
    {
        var response = await _client.GetAsync("/api/v1/auth/providers");
        var providers = await response.Content.ReadFromJsonAsync<SignInProvidersResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(providers!.Google);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Google_start_does_not_challenge_when_the_provider_is_disabled()
    {
        var response = await _client.GetAsync("/api/v1/auth/google/start");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
    }

    [Fact]
    public async Task Anonymous_password_reset_start_returns_a_generic_response()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/challenges",
            new { username = "missing" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
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
            builder.UseSetting("GoogleAuthentication:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IUserRepository, MissingUserRepository>();
                services.AddScoped<IAuditWriter, DiscardingAuditWriter>();
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
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

    private sealed class MissingUserRepository : IUserRepository
    {
        public Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public void Add(User user) => throw new NotSupportedException();
    }

    private sealed class DiscardingAuditWriter : IAuditWriter
    {
        public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task ExecuteSerializedTransactionAsync(
            string operationKey,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken) => operation(cancellationToken);
    }
}
