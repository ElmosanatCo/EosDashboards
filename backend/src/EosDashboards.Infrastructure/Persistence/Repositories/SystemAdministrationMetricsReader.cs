using EosDashboards.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class SystemAdministrationMetricsReader(EosDashboardDbContext context) : ISystemAdministrationMetricsReader
{
    private static readonly string[] FailedSecurityEventCodes =
    [
        "SignInDenied", "OtpVerificationFailed", "OtpVerificationInvalid", "OtpVerificationRejected",
        "OtpSendFailed", "OtpResendSendFailed", "OtpResendRejected", "SessionRefreshDenied",
        "PasswordResetRejected", "PasswordResetStartRejected",
    ];

    public async Task<SystemAdministrationMetrics> GetAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var activeUsers = await context.Users.CountAsync(user => user.IsActive, cancellationToken);
        var inactiveUsers = await context.Users.CountAsync(user => !user.IsActive, cancellationToken);
        var successfulSignIns = await context.AuditLogs.CountAsync(audit => audit.OccurredAt >= from && audit.OccurredAt < to && audit.Succeeded && audit.EventCode == "AuthenticationSucceeded", cancellationToken);
        var failedSecurityAttempts = await context.AuditLogs.CountAsync(audit => audit.OccurredAt >= from && audit.OccurredAt < to && !audit.Succeeded && FailedSecurityEventCodes.Contains(audit.EventCode), cancellationToken);
        var usersWithActiveSessions = await context.UserSessions.Where(session => session.RevokedAt == null && session.CreatedAt <= to && to < session.ExpiresAt).Select(session => session.UserId).Distinct().CountAsync(cancellationToken);
        return new SystemAdministrationMetrics(activeUsers, inactiveUsers, successfulSignIns, failedSecurityAttempts, usersWithActiveSessions);
    }
}
