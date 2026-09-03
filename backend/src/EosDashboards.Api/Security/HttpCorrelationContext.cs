using System.Net;
using EosDashboards.Application.Abstractions;

namespace EosDashboards.Api.Security;

public sealed class HttpCorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    public string TraceId => accessor.HttpContext?.TraceIdentifier ?? string.Empty;

    public string? ClientIpAddress
    {
        get
        {
            var address = accessor.HttpContext?.Connection.RemoteIpAddress;
            return address is null ? null : NormalizeIpAddress(address);
        }
    }

    public string? ClientDeviceKind => accessor.HttpContext is null
        ? null
        : ClassifyDevice(accessor.HttpContext.Request.Headers.UserAgent.ToString());

    private static string NormalizeIpAddress(IPAddress address)
    {
        var value = address.ToString();
        return value.Length <= 45 ? value : throw new InvalidOperationException("The client IP address is too long.");
    }

    private static string ClassifyDevice(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown";
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "Tablet";
        if (userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) return "Mobile";
        return "Desktop";
    }
}
