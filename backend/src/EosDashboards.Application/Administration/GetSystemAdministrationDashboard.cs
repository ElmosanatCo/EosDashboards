using EosDashboards.Application.Abstractions;

namespace EosDashboards.Application.Administration;

public sealed record SystemAdministrationDashboard(
    int ActiveUsers,
    int InactiveUsers,
    int SuccessfulSignIns,
    int FailedSecurityAttempts,
    int UsersWithActiveSessions)
{
    public override string ToString() => nameof(SystemAdministrationDashboard);
}

public sealed class GetSystemAdministrationDashboard(
    IClock clock,
    ISystemAdministrationMetricsReader metrics)
{
    private static readonly TimeSpan DashboardWindow = TimeSpan.FromHours(24);

    public async Task<SystemAdministrationDashboard> HandleAsync(CancellationToken cancellationToken)
    {
        var to = clock.Now;
        var values = await metrics.GetAsync(to.Subtract(DashboardWindow), to, cancellationToken);
        return new SystemAdministrationDashboard(values.ActiveUsers, values.InactiveUsers,
            values.SuccessfulSignIns, values.FailedSecurityAttempts, values.UsersWithActiveSessions);
    }
}
