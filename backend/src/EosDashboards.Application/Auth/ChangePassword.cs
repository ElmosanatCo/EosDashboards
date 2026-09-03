using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public enum ChangePasswordStatus
{
    Succeeded,
    Invalid,
}

public sealed record ChangePasswordResult(ChangePasswordStatus Status)
{
    public override string ToString() => nameof(ChangePasswordResult);
}

public sealed class ChangePassword(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IUserSessionRepository sessions,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<ChangePasswordResult> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(command.NewPassword))
        {
            return new ChangePasswordResult(ChangePasswordStatus.Invalid);
        }

        var now = clock.Now;
        var traceId = correlationContext.TraceId;
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null ||
            !user.IsActive ||
            user.Username is null ||
            user.PasswordHash is null ||
            passwordHasher.Verify(command.CurrentPassword, user.PasswordHash) is PasswordVerificationResult.Failed)
        {
            await WriteAuditAsync(command.UserId, false, traceId, cancellationToken);
            return new ChangePasswordResult(ChangePasswordStatus.Invalid);
        }

        user.SetLocalCredentials(user.Username, passwordHasher.Hash(command.NewPassword), now);
        var activeSessions = await sessions.GetActiveByUserIdAsync(user.Id, now, CancellationToken.None);
        foreach (var session in activeSessions)
        {
            session.Revoke(SessionRevocationReason.PasswordChanged, now);
        }

        await WriteAuditAsync(user.Id, true, traceId, CancellationToken.None);
        return new ChangePasswordResult(ChangePasswordStatus.Succeeded);
    }

    private async Task WriteAuditAsync(
        long? subjectUserId,
        bool succeeded,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(
                succeeded ? subjectUserId : null,
                subjectUserId,
                succeeded ? "PasswordChanged" : "PasswordChangeRejected",
                succeeded,
                traceId,
                null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
