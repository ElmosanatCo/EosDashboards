using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed record PasswordResetResult(PasswordResetStatus Status)
{
    public override string ToString() => nameof(PasswordResetResult);
}

public sealed class CompletePasswordReset(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IOtpChallengeRepository otpChallenges,
    IUserSessionRepository sessions,
    ISecretHasher secretHasher,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<PasswordResetResult> HandleAsync(
        CompletePasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(command.NewPassword))
        {
            return new PasswordResetResult(PasswordResetStatus.Invalid);
        }

        var traceId = correlationContext.TraceId;
        var now = clock.Now;
        var challenge = await otpChallenges.FindByPublicTokenAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null ||
            challenge.Purpose != OtpChallengePurpose.PasswordReset ||
            challenge.Status != OtpChallengeStatus.Sent)
        {
            await WriteAuditAsync(null, "PasswordResetRejected", false, traceId, cancellationToken);
            return new PasswordResetResult(PasswordResetStatus.Invalid);
        }

        var user = await users.GetForUpdateAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.Username is null || !challenge.Verify(secretHasher.Hash(command.Code), now))
        {
            await WriteAuditAsync(challenge.UserId, "PasswordResetRejected", false, traceId, CancellationToken.None);
            return new PasswordResetResult(PasswordResetStatus.Invalid);
        }

        user.CompleteTemporaryPasswordChange(passwordHasher.Hash(command.NewPassword), now);
        var activeSessions = await sessions.GetActiveByUserIdAsync(user.Id, now, CancellationToken.None);
        foreach (var session in activeSessions)
        {
            session.Revoke(SessionRevocationReason.PasswordChanged, now);
        }

        await WriteAuditAsync(user.Id, "PasswordResetSucceeded", true, traceId, CancellationToken.None);
        return new PasswordResetResult(PasswordResetStatus.Succeeded);
    }

    private async Task WriteAuditAsync(
        long? subjectUserId,
        string eventCode,
        bool succeeded,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(null, subjectUserId, eventCode, succeeded, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
