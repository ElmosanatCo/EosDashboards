using EosDashboards.Application.Abstractions;

namespace EosDashboards.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTime Now
    {
        get
        {
            var local = DateTime.Now;
            return DateTime.SpecifyKind(
                local.AddTicks(-(local.Ticks % TimeSpan.TicksPerMillisecond)),
                DateTimeKind.Unspecified);
        }
    }
}
