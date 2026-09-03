using Microsoft.AspNetCore.Diagnostics;

namespace EosDashboards.Api.Errors;

public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isClientError = exception is ArgumentException;
        if (isClientError)
        {
            logger.LogInformation("Request validation failed. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }

        var result = ApiResults.Problem(
            httpContext,
            isClientError ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
            isClientError ? "invalid_request" : "unexpected_error",
            isClientError ? "The request is invalid." : "The request could not be completed.");
        await result.ExecuteAsync(httpContext);
        return true;
    }
}
