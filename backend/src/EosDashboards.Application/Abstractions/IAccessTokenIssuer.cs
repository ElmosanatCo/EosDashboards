using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Abstractions;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(
        User user,
        long sessionId,
        DateTime issuedAt,
        DateTime expiresAt);
}
