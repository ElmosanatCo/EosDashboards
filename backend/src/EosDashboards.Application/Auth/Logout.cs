using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class Logout(
    IClock clock,
    IUserSessionRepository sessions,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null || session.RevokedAtUtc.HasValue)
        {
            return;
        }

        session.Revoke(SessionRevocationReason.UserLogout, clock.UtcNow);
        await auditWriter.WriteAsync(
            new AuditRecord(
                session.UserId,
                session.UserId,
                "UserLogout",
                true,
                Guid.NewGuid().ToString("N"),
                null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
