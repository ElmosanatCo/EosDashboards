using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class StartPasswordReset(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IOtpChallengeRepository otpChallenges,
    ISmsSender smsSender,
    ISecretHasher secretHasher,
    ISecureTokenGenerator tokenGenerator,
    IMobileProtector mobileProtector,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public async Task<PasswordResetStartResult> HandleAsync(
        StartPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        var publicToken = tokenGenerator.CreateOpaqueToken(32);
        var expiresAtUtc = now.Add(ChallengeLifetime);
        var user = await users.FindByUsernameAsync(NormalizeUsername(command.Username), cancellationToken);
        if (user is null || !user.IsActive || user.PasswordHash is null)
        {
            await WriteAuditAsync(null, "PasswordResetRequested", false, traceId, cancellationToken);
            return new PasswordResetStartResult(PasswordResetStartStatus.Succeeded, publicToken, expiresAtUtc);
        }

        var latest = await otpChallenges.FindLatestActiveAsync(
            user.Id,
            OtpChallengePurpose.PasswordReset,
            cancellationToken);
        if (latest is not null && now < latest.ResendAvailableAtUtc)
        {
            await WriteAuditAsync(user.Id, "PasswordResetCooldown", false, traceId, cancellationToken);
            return new PasswordResetStartResult(
                PasswordResetStartStatus.Succeeded,
                latest.PublicToken,
                latest.ExpiresAtUtc);
        }

        latest?.Supersede();
        var code = tokenGenerator.CreateSixDigitCode();
        var challenge = OtpChallenge.Create(
            user.Id,
            publicToken,
            secretHasher.Hash(code),
            now,
            expiresAtUtc,
            OtpChallengePurpose.PasswordReset);
        otpChallenges.Add(challenge);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        SmsSendResult sendResult;
        try
        {
            sendResult = await smsSender.SendAsync(
                new SmsMessage(
                    mobileProtector.Unprotect(user.ProtectedMobileNumber),
                    $"EosDashboards password reset code: {code}"),
                cancellationToken);
        }
        catch (TimeoutException)
        {
            sendResult = new SmsSendResult(false, "timeout");
        }

        if (!sendResult.Succeeded)
        {
            challenge.MarkSendFailed();
            await WriteAuditAsync(user.Id, "PasswordResetOtpSendFailed", false, traceId, cancellationToken);
            return new PasswordResetStartResult(PasswordResetStartStatus.DependencyUnavailable, publicToken, expiresAtUtc);
        }

        challenge.MarkSent();
        await WriteAuditAsync(user.Id, "PasswordResetOtpSent", true, traceId, cancellationToken);
        return new PasswordResetStartResult(PasswordResetStartStatus.Succeeded, publicToken, expiresAtUtc);
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

    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}
