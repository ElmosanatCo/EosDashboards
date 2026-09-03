using EosDashboards.Application.Abstractions;

namespace EosDashboards.Api.Security;

public sealed class HttpCorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    public string TraceId => accessor.HttpContext?.TraceIdentifier ?? string.Empty;
}
