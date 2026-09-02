using EosDashboards.Application.Abstractions;

namespace EosDashboards.Application.Auth;

public sealed class RefreshSession(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IUserSessionRepository sessions,
    ISecretHasher secretHasher,
    ISecureTokenGenerator tokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);

    public async Task<RefreshSessionResult> HandleAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        var refreshHash = secretHasher.Hash(command.RefreshCredential);
        var session = await sessions.FindByRefreshHashAsync(refreshHash, cancellationToken);

        if (session is null || !session.IsActive(now))
        {
            await WriteDenialAsync(session?.UserId, traceId, cancellationToken);
            return Denied();
        }

        var user = await users.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await WriteDenialAsync(session.UserId, traceId, cancellationToken);
            return Denied();
        }

        var replacementCredential = tokenGenerator.CreateOpaqueToken(32);
        session.Rotate(secretHasher.Hash(replacementCredential), now);
        await auditWriter.WriteAsync(
            new AuditRecord(user.Id, user.Id, "SessionRefreshed", true, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessTokenExpiresAtUtc = now.Add(AccessTokenLifetime) < session.ExpiresAtUtc
            ? now.Add(AccessTokenLifetime)
            : session.ExpiresAtUtc;
        return new RefreshSessionResult(
            RefreshSessionStatus.Succeeded,
            accessTokenIssuer.Issue(user, session.Id, now, accessTokenExpiresAtUtc),
            replacementCredential,
            session.ExpiresAtUtc,
            VerifyOtp.Project(user));
    }

    private async Task WriteDenialAsync(
        long? subjectUserId,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(subjectUserId, subjectUserId, "SessionRefreshDenied", false, traceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static RefreshSessionResult Denied()
    {
        return new RefreshSessionResult(RefreshSessionStatus.Denied, null, null, null, null);
    }
}
