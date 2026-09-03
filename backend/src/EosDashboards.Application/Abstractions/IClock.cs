namespace EosDashboards.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
