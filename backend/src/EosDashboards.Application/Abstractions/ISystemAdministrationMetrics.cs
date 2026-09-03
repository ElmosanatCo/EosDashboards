namespace EosDashboards.Application.Abstractions;

public sealed record SystemAdministrationMetrics(
    int ActiveUsers,
    int InactiveUsers,
    int SuccessfulSignIns,
    int FailedSecurityAttempts,
    int UsersWithActiveSessions);

public interface ISystemAdministrationMetricsReader
{
    Task<SystemAdministrationMetrics> GetAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
