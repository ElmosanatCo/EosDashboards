namespace EosDashboards.Application.Abstractions;

public interface ICorrelationContext
{
    string TraceId { get; }
}
