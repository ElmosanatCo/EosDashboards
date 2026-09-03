using EosDashboards.Application.Abstractions;

namespace EosDashboards.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
