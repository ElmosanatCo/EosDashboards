namespace EosDashboards.Application.Abstractions;

public interface IClock
{
    DateTime Now { get; }
}
