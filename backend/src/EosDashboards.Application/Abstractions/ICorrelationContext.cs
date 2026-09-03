namespace EosDashboards.Application.Abstractions;

public interface ICorrelationContext
{
    string TraceId { get; }

    string? ClientIpAddress => null;

    string? ClientDeviceKind => null;
}
