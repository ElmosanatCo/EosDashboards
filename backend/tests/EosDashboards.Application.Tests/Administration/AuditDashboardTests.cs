using EosDashboards.Application.Administration;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Tests.Auth;

namespace EosDashboards.Application.Tests.Administration;

public sealed class AuditDashboardTests
{
    [Fact]
    public async Task Dashboard_counts_only_successful_and_failed_security_events_in_the_last_24_hours()
    {
        var context = new AuditDashboardContext();

        var dashboard = await context.UseCase.HandleAsync(CancellationToken.None);

        Assert.Equal(2, dashboard.SuccessfulSignIns);
        Assert.Equal(1, dashboard.FailedSecurityAttempts);
        Assert.Equal(3, dashboard.UsersWithActiveSessions);
        Assert.Equal(4, dashboard.ActiveUsers);
        Assert.Equal(2, dashboard.InactiveUsers);
        Assert.Equal(context.Clock.Now.AddHours(-24), context.Audits.LastFrom);
        Assert.Equal(context.Clock.Now, context.Audits.LastTo);
    }

    private sealed class AuditDashboardContext
    {
        public AuditDashboardContext()
        {
            Audits.SuccessfulSignIns = 2;
            Audits.FailedSecurityAttempts = 1;
            Audits.ActiveUsers = 4;
            Audits.InactiveUsers = 2;
            Audits.UsersWithActiveSessions = 3;
            UseCase = new GetSystemAdministrationDashboard(Clock, Audits);
        }

        public FakeClock Clock { get; } = new(new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Unspecified));
        public TestAuditLogRepository Audits { get; } = new();
        public GetSystemAdministrationDashboard UseCase { get; }
    }
}

internal sealed class TestAuditLogRepository : ISystemAdministrationMetricsReader
{
    public int SuccessfulSignIns { get; set; }
    public int FailedSecurityAttempts { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int UsersWithActiveSessions { get; set; }
    public DateTime LastFrom { get; private set; }
    public DateTime LastTo { get; private set; }

    public Task<SystemAdministrationMetrics> GetAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        LastFrom = from;
        LastTo = to;
        return Task.FromResult(new SystemAdministrationMetrics(
            ActiveUsers, InactiveUsers, SuccessfulSignIns, FailedSecurityAttempts, UsersWithActiveSessions));
    }
}
