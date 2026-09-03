using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class Logout(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserSessionRepository sessions,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null || session.RevokedAt.HasValue)
        {
            return;
        }

        session.Revoke(SessionRevocationReason.UserLogout, clock.Now);
        await auditWriter.WriteAsync(
            new AuditRecord(
                session.UserId,
                session.UserId,
                "UserLogout",
                true,
                correlationContext.TraceId,
                null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
