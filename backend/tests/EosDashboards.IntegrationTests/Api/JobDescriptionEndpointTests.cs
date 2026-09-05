using System.Net;
using System.Net.Http.Json;

namespace EosDashboards.IntegrationTests.Api;

public sealed class JobDescriptionEndpointTests : IClassFixture<AuthEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;

    public JobDescriptionEndpointTests(AuthEndpointTests.ApiFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Theory]
    [InlineData("/api/v1/job-descriptions/human-resources-dashboard")]
    [InlineData("/api/v1/job-descriptions/human-resources-departments")]
    [InlineData("/api/v1/job-descriptions/human-resources-review")]
    [InlineData("/api/v1/job-descriptions/human-resources-approved")]
    [InlineData("/api/v1/job-descriptions/1/comparison")]
    public async Task Human_resources_read_routes_reject_anonymous_access(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Public_skill_merge_rejects_anonymous_access()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/job-descriptions/catalog/public-skills/1/merge",
            new { survivingSkillId = 2 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
