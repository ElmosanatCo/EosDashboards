using EosDashboards.Application.Abstractions;

namespace EosDashboards.Application.Administration;

public sealed record SystemAdministrationDashboard(
    int ActiveUsers,
    int InactiveUsers,
    int SuccessfulSignIns,
    int FailedSecurityAttempts,
    int UsersWithActiveSessions,
    IReadOnlyList<AuditLogListItem> LatestAuditLogs)
{
    public override string ToString() => nameof(SystemAdministrationDashboard);
}

public sealed class GetSystemAdministrationDashboard(
    IClock clock,
    ISystemAdministrationMetricsReader metrics,
    IAuditLogReader auditLogs)
{
    private static readonly TimeSpan DashboardWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan RecentAuditWindow = TimeSpan.FromDays(7);

    public async Task<SystemAdministrationDashboard> HandleAsync(CancellationToken cancellationToken)
    {
        var to = clock.Now;
        var values = await metrics.GetAsync(to.Subtract(DashboardWindow), to, cancellationToken);
        var recentAudits = await auditLogs.QueryAsync(new AuditLogQuery(
            to.Subtract(RecentAuditWindow), to, null, null, null, null, 1, 10), cancellationToken);
        return new SystemAdministrationDashboard(values.ActiveUsers, values.InactiveUsers,
            values.SuccessfulSignIns, values.FailedSecurityAttempts, values.UsersWithActiveSessions, recentAudits.Items);
    }
}
