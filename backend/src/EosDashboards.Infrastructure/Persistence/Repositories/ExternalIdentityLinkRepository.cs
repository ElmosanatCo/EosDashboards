using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class ExternalIdentityLinkRepository(EosDashboardDbContext context) : IExternalIdentityLinkRepository
{
    public Task<ExternalIdentityLink?> FindByProviderSubjectAsync(
        ExternalIdentityProvider provider,
        string providerSubject,
        CancellationToken cancellationToken) =>
        context.Set<ExternalIdentityLink>().SingleOrDefaultAsync(
            link => link.Provider == provider && link.ProviderSubject == providerSubject,
            cancellationToken);

    public Task<ExternalIdentityLink?> FindPendingByProviderEmailAsync(
        ExternalIdentityProvider provider,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        context.Set<ExternalIdentityLink>().SingleOrDefaultAsync(
            link => link.Provider == provider &&
                    link.NormalizedEmail == normalizedEmail &&
                    link.ProviderSubject == null,
            cancellationToken);

    public Task<ExternalIdentityLink?> FindByUserIdAndProviderAsync(
        long userId,
        ExternalIdentityProvider provider,
        CancellationToken cancellationToken) =>
        context.Set<ExternalIdentityLink>().SingleOrDefaultAsync(
            link => link.UserId == userId && link.Provider == provider,
            cancellationToken);

    public void Add(ExternalIdentityLink link) => context.Add(link);
}
