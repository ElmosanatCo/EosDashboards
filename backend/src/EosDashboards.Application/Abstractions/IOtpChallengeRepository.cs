using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IOtpChallengeRepository
{
    Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken);

    Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken);

    void Add(OtpChallenge challenge);
}
