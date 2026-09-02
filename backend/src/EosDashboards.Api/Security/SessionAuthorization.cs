using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace EosDashboards.Api.Security;

public sealed class ActiveSessionRequirement : IAuthorizationRequirement;

public sealed class SystemAdministratorRequirement : IAuthorizationRequirement;

public sealed class SessionAuthorizationHandler(
    IClock clock,
    IUserRepository users,
    IUserSessionRepository sessions,
    IRoleRepository roles)
    : AuthorizationHandler<IAuthorizationRequirement>
{
    public const string SystemAdministratorRoleCode = "SystemAdministrator";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        if (!TryReadId(context.User, JwtRegisteredClaimNames.Sub, out var userId) ||
            !TryReadId(context.User, JwtRegisteredClaimNames.Sid, out var sessionId))
        {
            return;
        }

        var session = await sessions.GetByIdAsync(sessionId, CancellationToken.None);
        var user = await users.GetByIdAsync(userId, CancellationToken.None);
        if (session is null || session.UserId != userId || !session.IsActive(clock.UtcNow) ||
            user is null || !user.IsActive)
        {
            return;
        }

        if (requirement is ActiveSessionRequirement)
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement is SystemAdministratorRequirement)
        {
            var role = await roles.FindByCodeAsync(SystemAdministratorRoleCode, CancellationToken.None);
            if (role is not null && role.IsActive && role.IsSystem &&
                string.Equals(role.Code, SystemAdministratorRoleCode, StringComparison.Ordinal) &&
                user.UserRoles.Any(userRole => userRole.RoleId == role.Id))
            {
                context.Succeed(requirement);
            }
        }
    }

    public static bool TryReadId(System.Security.Claims.ClaimsPrincipal principal, string claimType, out long id)
    {
        return long.TryParse(
            principal.FindFirst(claimType)?.Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out id) && id > 0;
    }
}
