using EosDashboards.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EosDashboards.IntegrationTests.Api;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task Google_start_unavailability_redirects_to_the_safe_sign_in_message()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/google/start";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        context.RequestServices = services.BuildServiceProvider();

        var handled = await new ExceptionHandler(NullLogger<ExceptionHandler>.Instance)
            .TryHandleAsync(context, new InvalidOperationException("Google metadata is unavailable."), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/EosDashboards/?authError=google-unavailable", context.Response.Headers.Location);
    }
}
