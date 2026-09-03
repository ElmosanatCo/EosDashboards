using System.Net;

namespace EosDashboards.IntegrationTests.Api;

public sealed class AdministrationEndpointTests : IClassFixture<AuthEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;

    public AdministrationEndpointTests(AuthEndpointTests.ApiFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Theory]
    [InlineData("/api/v1/administration/dashboard")]
    [InlineData("/api/v1/administration/audit-logs")]
    public async Task System_administration_routes_reject_anonymous_access(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
