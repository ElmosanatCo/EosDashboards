using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class VerifyOtp(
    IClock clock,
    IUserRepository users,
    IOtpChallengeRepository otpChallenges,
    IUserSessionRepository sessions,
    ISecretHasher secretHasher,
    ISecureTokenGenerator tokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<AuthenticationResult> HandleAsync(
        VerifyOtpCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var traceId = Guid.NewGuid().ToString("N");
        var challenge = await otpChallenges.FindByPublicTokenAsync(
            command.ChallengeToken,
            cancellationToken);

        if (challenge is null)
        {
            await WriteFailureAsync(null, "OtpVerificationInvalid", traceId, cancellationToken);
            return Failed(VerifyOtpStatus.Invalid);
        }

        if (challenge.Status != OtpChallengeStatus.Sent)
        {
            var status = MapStatus(challenge.Status);
            await WriteFailureAsync(challenge.UserId, "OtpVerificationRejected", traceId, cancellationToken);
            return Failed(status);
        }

        var user = await users.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await WriteFailureAsync(user?.Id, "OtpVerificationInvalid", traceId, cancellationToken);
            return Failed(VerifyOtpStatus.Invalid);
        }

        var verified = challenge.Verify(secretHasher.Hash(command.Code), now);
        if (!verified)
        {
            var status = MapStatus(challenge.Status);
            await auditWriter.WriteAsync(
                new AuditRecord(user.Id, user.Id, "OtpVerificationFailed", false, traceId, null),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Failed(status);
        }

        var refreshCredential = tokenGenerator.CreateOpaqueToken(32);
        var session = UserSession.Create(user.Id, secretHasher.Hash(refreshCredential), now);
        sessions.Add(session);
        await auditWriter.WriteAsync(
            new AuditRecord(user.Id, user.Id, "AuthenticationSucceeded", true, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = accessTokenIssuer.Issue(user, session.Id, now);
        return new AuthenticationResult(
            VerifyOtpStatus.Succeeded,
            accessToken,
            refreshCredential,
            session.ExpiresAtUtc,
            Project(user));
    }

    private async Task WriteFailureAsync(
        long? subjectUserId,
        string eventCode,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(subjectUserId, subjectUserId, eventCode, false, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static VerifyOtpStatus MapStatus(OtpChallengeStatus status)
    {
        return status switch
        {
            OtpChallengeStatus.Expired => VerifyOtpStatus.Expired,
            OtpChallengeStatus.Exhausted => VerifyOtpStatus.Exhausted,
            OtpChallengeStatus.Consumed => VerifyOtpStatus.Consumed,
            _ => VerifyOtpStatus.Invalid,
        };
    }

    private static AuthenticationResult Failed(VerifyOtpStatus status)
    {
        return new AuthenticationResult(status, null, null, null, null);
    }

    internal static AuthenticatedUser Project(User user)
    {
        return new AuthenticatedUser(
            user.Id,
            user.AccountName,
            user.FirstName,
            user.LastName,
            user.UserRoles.Select(role => role.RoleId).ToArray());
    }
}
