using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Auth;

public sealed class GoogleSignIn(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IRoleRepository roles,
    IDepartmentRepository departments,
    IExternalIdentityLinkRepository externalIdentityLinks,
    IUserSessionRepository sessions,
    ISecretHasher secretHasher,
    ISecureTokenGenerator tokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private const string OperationKey = "GoogleSignIn";
    private const string DeniedEventCode = "GoogleAuthenticationDenied";
    private const string SucceededEventCode = "GoogleAuthenticationSucceeded";

    public async Task<GoogleSignInResult> HandleAsync(
        GoogleIdentity identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.EmailVerified ||
            !TryNormalize(identity, out var subject, out var normalizedEmail))
        {
            await WriteDeniedAsync(null, cancellationToken);
            return Denied();
        }

        cancellationToken.ThrowIfCancellationRequested();
        GoogleSignInResult? result = null;
        await unitOfWork.ExecuteSerializedTransactionAsync(
            OperationKey,
            async transactionCancellationToken =>
            {
                var link = await externalIdentityLinks.FindByProviderSubjectAsync(
                    ExternalIdentityProvider.Google,
                    subject,
                    transactionCancellationToken) ??
                    await externalIdentityLinks.FindPendingByProviderEmailAsync(
                        ExternalIdentityProvider.Google,
                        normalizedEmail,
                        transactionCancellationToken);
                if (link is null)
                {
                    result = await DenyInTransactionAsync(null, transactionCancellationToken);
                    return;
                }

                var user = await users.GetByIdAsync(link.UserId, transactionCancellationToken);
                if (user is null || !user.IsActive)
                {
                    result = await DenyInTransactionAsync(link.UserId, transactionCancellationToken);
                    return;
                }

                var now = clock.Now;
                if (link.ProviderSubject is null)
                {
                    link.BindSubject(subject, now);
                }

                var refreshCredential = tokenGenerator.CreateOpaqueToken(32);
                var session = UserSession.Create(user.Id, secretHasher.Hash(refreshCredential), now);
                sessions.Add(session);
                await CommitAuthenticationAsync(
                    new AuditRecord(user.Id, user.Id, SucceededEventCode, true, correlationContext.TraceId, null));

                var accessToken = accessTokenIssuer.Issue(user, session.Id, now, now.AddMinutes(10));
                result = new GoogleSignInResult(
                    GoogleSignInStatus.Succeeded,
                    new AuthenticationResult(
                        VerifyOtpStatus.Succeeded,
                        accessToken,
                        refreshCredential,
                        session.ExpiresAt,
                        await VerifyOtp.ProjectAsync(
                            user,
                            roles,
                            departments,
                            transactionCancellationToken)));
            },
            CancellationToken.None);

        return result ?? Denied();
    }

    private async Task<GoogleSignInResult> DenyInTransactionAsync(
        long? subjectUserId,
        CancellationToken cancellationToken)
    {
        await WriteDeniedAsync(subjectUserId, cancellationToken);
        return Denied();
    }

    private async Task WriteDeniedAsync(long? subjectUserId, CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditRecord(null, subjectUserId, DeniedEventCode, false, correlationContext.TraceId, null),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task CommitAuthenticationAsync(AuditRecord auditRecord)
    {
        try
        {
            await auditWriter.WriteAsync(auditRecord, CancellationToken.None);
        }
        catch
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static GoogleSignInResult Denied() => new(GoogleSignInStatus.Denied, null);

    private static bool TryNormalize(
        GoogleIdentity identity,
        out string subject,
        out string normalizedEmail)
    {
        subject = identity.Subject?.Trim() ?? string.Empty;
        normalizedEmail = string.Empty;
        if (subject.Length is 0 or > 255)
        {
            return false;
        }

        try
        {
            normalizedEmail = ExternalIdentityLink.NormalizeEmail(identity.Email);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
