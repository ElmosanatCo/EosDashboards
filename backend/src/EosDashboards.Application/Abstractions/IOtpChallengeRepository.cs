using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Abstractions;

public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken);

    Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken);

    Task<OtpChallenge?> FindLatestActiveAsync(
        long userId,
        OtpChallengePurpose purpose,
        CancellationToken cancellationToken);

    void Add(OtpChallenge challenge);
}
