using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Abstractions;

public interface IExternalIdentityLinkRepository
{
    Task<ExternalIdentityLink?> FindByProviderSubjectAsync(
        ExternalIdentityProvider provider,
        string providerSubject,
        CancellationToken cancellationToken);

    Task<ExternalIdentityLink?> FindPendingByProviderEmailAsync(
        ExternalIdentityProvider provider,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<ExternalIdentityLink?> FindByUserIdAndProviderAsync(
        long userId,
        ExternalIdentityProvider provider,
        CancellationToken cancellationToken);

    void Add(ExternalIdentityLink link);
}
