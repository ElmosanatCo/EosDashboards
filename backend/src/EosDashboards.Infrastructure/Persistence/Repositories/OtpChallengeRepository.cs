using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class OtpChallengeRepository(EosDashboardDbContext context) : IOtpChallengeRepository
{
    public Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken) =>
        context.OtpChallenges.SingleOrDefaultAsync(
            challenge => challenge.PublicToken == token,
            cancellationToken);

    public Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken) =>
        context.OtpChallenges
            .Where(challenge =>
                challenge.UserId == userId &&
                (challenge.Status == OtpChallengeStatus.Pending ||
                 challenge.Status == OtpChallengeStatus.Sent))
            .OrderByDescending(challenge => challenge.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(OtpChallenge challenge) => context.OtpChallenges.Add(challenge);
}
