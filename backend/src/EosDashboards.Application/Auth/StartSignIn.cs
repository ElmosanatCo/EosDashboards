using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class StartSignIn(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IOtpChallengeRepository otpChallenges,
    ISmsSender smsSender,
    ISecretHasher secretHasher,
    IPasswordHasher passwordHasher,
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
        var now = clock.Now;
        var traceId = correlationContext.TraceId;
        if (!PasswordPolicy.IsValid(command.Password))
        {
            return await DeniedAsync(null, traceId, cancellationToken);
        }

        var user = await users.FindByUsernameAsync(
            NormalizeUsername(command.Username),
            cancellationToken);

        var passwordVerification = user is null || user.PasswordHash is null
            ? PasswordVerificationResult.Failed
            : passwordHasher.Verify(command.Password, user.PasswordHash);
        if (user is null ||
            !user.IsActive ||
            user.PasswordHash is null ||
            passwordVerification is PasswordVerificationResult.Failed)
        {
            return await DeniedAsync(user, traceId, cancellationToken);
        }

        if (passwordVerification is PasswordVerificationResult.RehashNeeded)
        {
            user.SetLocalCredentials(
                user.Username!,
                passwordHasher.Hash(command.Password),
                now);
        }

        var latestChallenge = await otpChallenges.FindLatestActiveAsync(user.Id, cancellationToken);
        if (latestChallenge is not null && now < latestChallenge.ResendAvailableAt)
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
                latestChallenge.ResendAvailableAt);
        }

        latestChallenge?.Supersede();
        return await IssueChallengeAsync(user, now, traceId, "OtpSent", "OtpSendFailed", cancellationToken);
    }

    public async Task<StartSignInResult> ResendAsync(
        ResendOtpCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var traceId = correlationContext.TraceId;
        var previous = await otpChallenges.FindByPublicTokenAsync(command.ChallengeToken, cancellationToken);
        if (previous is null ||
            previous.Purpose != OtpChallengePurpose.SignIn ||
            previous.Status != OtpChallengeStatus.Sent ||
            now >= previous.ExpiresAt)
        {
            return await ResendDeniedAsync(null, traceId, cancellationToken);
        }

        if (now < previous.ResendAvailableAt)
        {
            await auditWriter.WriteAsync(
                new AuditRecord(null, previous.UserId, "OtpResendCooldown", false, traceId, null),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new StartSignInResult(
                StartSignInStatus.Cooldown,
                null,
                null,
                null,
                previous.ResendAvailableAt);
        }

        var user = await users.GetByIdAsync(previous.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.PasswordHash is null)
        {
            return await ResendDeniedAsync(previous.UserId, traceId, cancellationToken);
        }

        previous.Supersede();
        return await IssueChallengeAsync(user, now, traceId, "OtpResent", "OtpResendSendFailed", cancellationToken);
    }

    private async Task<StartSignInResult> IssueChallengeAsync(
        User user,
        DateTime now,
        string traceId,
        string sentAuditEvent,
        string failedAuditEvent,
        CancellationToken cancellationToken)
    {
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
                    $"داشبورد علم و صنعت، کد تأیید شما: {code}"),
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
                    failedAuditEvent,
                    false,
                    traceId,
                    SafeErrorMetadata(sendResult.SafeErrorCode)),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DependencyUnavailable();
        }

        challenge.MarkSent();
        await auditWriter.WriteAsync(
            new AuditRecord(null, user.Id, sentAuditEvent, true, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartSignInResult(
            StartSignInStatus.Succeeded,
            challenge.PublicToken,
            user.MaskedMobileNumber,
            challenge.ExpiresAt,
            challenge.ResendAvailableAt);
    }

    private async Task<StartSignInResult> DeniedAsync(
        User? user,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(null, user?.Id, "SignInDenied", false, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new StartSignInResult(StartSignInStatus.Denied, null, null, null, null);
    }

    private async Task<StartSignInResult> ResendDeniedAsync(
        long? userId,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(null, userId, "OtpResendRejected", false, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new StartSignInResult(StartSignInStatus.Denied, null, null, null, null);
    }

    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();

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
