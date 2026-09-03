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
            return new PasswordResetStartResult(
                PasswordResetStartStatus.Succeeded,
                publicToken,
                expiresAtUtc,
                now.AddSeconds(60));
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
                latest.ExpiresAtUtc,
                latest.ResendAvailableAtUtc);
        }

        latest?.Supersede();
        return await IssueChallengeAsync(
            user,
            publicToken,
            now,
            traceId,
            "PasswordResetOtpSent",
            "PasswordResetOtpSendFailed",
            cancellationToken);
    }

    public async Task<PasswordResetStartResult> ResendAsync(
        ResendOtpCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        var previous = await otpChallenges.FindByPublicTokenAsync(command.ChallengeToken, cancellationToken);
        if (previous is null ||
            previous.Purpose != OtpChallengePurpose.PasswordReset ||
            previous.Status != OtpChallengeStatus.Sent ||
            now >= previous.ExpiresAtUtc)
        {
            return await GenericResultAsync(now, traceId, "PasswordResetResendRequested", cancellationToken);
        }

        if (now < previous.ResendAvailableAtUtc)
        {
            await WriteAuditAsync(previous.UserId, "PasswordResetCooldown", false, traceId, cancellationToken);
            return new PasswordResetStartResult(
                PasswordResetStartStatus.Succeeded,
                previous.PublicToken,
                previous.ExpiresAtUtc,
                previous.ResendAvailableAtUtc);
        }

        var user = await users.GetByIdAsync(previous.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.PasswordHash is null)
        {
            return await GenericResultAsync(now, traceId, "PasswordResetResendRequested", cancellationToken);
        }

        previous.Supersede();
        return await IssueChallengeAsync(
            user,
            tokenGenerator.CreateOpaqueToken(32),
            now,
            traceId,
            "PasswordResetOtpResent",
            "PasswordResetOtpResendFailed",
            cancellationToken);
    }

    private async Task<PasswordResetStartResult> IssueChallengeAsync(
        User user,
        string publicToken,
        DateTimeOffset now,
        string traceId,
        string sentAuditEvent,
        string failedAuditEvent,
        CancellationToken cancellationToken)
    {
        var expiresAtUtc = now.Add(ChallengeLifetime);
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
                    $"داشبورد علم و صنعت، کد بازیابی رمز عبور شما: {code}"),
                cancellationToken);
        }
        catch (TimeoutException)
        {
            sendResult = new SmsSendResult(false, "timeout");
        }

        if (!sendResult.Succeeded)
        {
            challenge.MarkSendFailed();
            await WriteAuditAsync(user.Id, failedAuditEvent, false, traceId, cancellationToken);
            return new PasswordResetStartResult(
                PasswordResetStartStatus.DependencyUnavailable,
                publicToken,
                expiresAtUtc,
                challenge.ResendAvailableAtUtc);
        }

        challenge.MarkSent();
        await WriteAuditAsync(user.Id, sentAuditEvent, true, traceId, cancellationToken);
        return new PasswordResetStartResult(
            PasswordResetStartStatus.Succeeded,
            publicToken,
            expiresAtUtc,
            challenge.ResendAvailableAtUtc);
    }

    private async Task<PasswordResetStartResult> GenericResultAsync(
        DateTimeOffset now,
        string traceId,
        string auditEvent,
        CancellationToken cancellationToken)
    {
        await WriteAuditAsync(null, auditEvent, false, traceId, cancellationToken);
        return new PasswordResetStartResult(
            PasswordResetStartStatus.Succeeded,
            tokenGenerator.CreateOpaqueToken(32),
            now.Add(ChallengeLifetime),
            now.AddSeconds(60));
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
