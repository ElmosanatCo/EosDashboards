using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Auth;

public sealed class StartSignIn(
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

    public async Task<StartSignInResult> HandleAsync(
        StartSignInCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        var user = await users.FindByOrganizationalIdAsync(
            command.Identity.StableId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            await auditWriter.WriteAsync(
                new AuditRecord(null, user?.Id, "SignInDenied", false, traceId, null),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Denied();
        }

        var latestChallenge = await otpChallenges.FindLatestActiveAsync(user.Id, cancellationToken);
        if (latestChallenge is not null && now < latestChallenge.ResendAvailableAtUtc)
        {
            await auditWriter.WriteAsync(
                new AuditRecord(null, user.Id, "OtpResendCooldown", false, traceId, null),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new StartSignInResult(
                StartSignInStatus.Cooldown,
                null,
                null,
                null,
                latestChallenge.ResendAvailableAtUtc);
        }

        latestChallenge?.Supersede();
        var code = tokenGenerator.CreateSixDigitCode();
        var publicToken = tokenGenerator.CreateOpaqueToken(32);
        var challenge = OtpChallenge.Create(
            user.Id,
            publicToken,
            secretHasher.Hash(code),
            now,
            now.Add(ChallengeLifetime));
        otpChallenges.Add(challenge);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        SmsSendResult sendResult;
        try
        {
            sendResult = await smsSender.SendAsync(
                new SmsMessage(
                    mobileProtector.Unprotect(user.ProtectedMobileNumber),
                    $"EosDashboards verification code: {code}"),
                cancellationToken);
        }
        catch (TimeoutException)
        {
            sendResult = new SmsSendResult(false, "timeout");
        }

        if (!sendResult.Succeeded)
        {
            challenge.MarkSendFailed();
            await auditWriter.WriteAsync(
                new AuditRecord(
                    null,
                    user.Id,
                    "OtpSendFailed",
                    false,
                    traceId,
                    SafeErrorMetadata(sendResult.SafeErrorCode)),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DependencyUnavailable();
        }

        challenge.MarkSent();
        await auditWriter.WriteAsync(
            new AuditRecord(null, user.Id, "OtpSent", true, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartSignInResult(
            StartSignInStatus.Succeeded,
            challenge.PublicToken,
            user.MaskedMobileNumber,
            challenge.ExpiresAtUtc,
            challenge.ResendAvailableAtUtc);
    }

    private static StartSignInResult Denied()
    {
        return new StartSignInResult(StartSignInStatus.Denied, null, null, null, null);
    }

    private static StartSignInResult DependencyUnavailable()
    {
        return new StartSignInResult(
            StartSignInStatus.DependencyUnavailable,
            null,
            null,
            null,
            null);
    }

    private static IReadOnlyDictionary<string, string>? SafeErrorMetadata(string? safeErrorCode)
    {
        return string.IsNullOrWhiteSpace(safeErrorCode)
            ? null
            : new Dictionary<string, string> { ["errorCode"] = safeErrorCode };
    }
}
