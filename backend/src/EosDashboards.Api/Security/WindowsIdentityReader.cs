using System.Security.Claims;
using EosDashboards.Application.Auth;

namespace EosDashboards.Api.Security;

public interface IWindowsIdentityReader
{
    OrganizationalIdentity? Read(ClaimsPrincipal principal);
}

public sealed class WindowsIdentityReader : IWindowsIdentityReader
{
    public OrganizationalIdentity? Read(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var stableId = principal.FindFirstValue(ClaimTypes.PrimarySid) ??
            principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var accountName = principal.Identity.Name;
        return string.IsNullOrWhiteSpace(stableId) || string.IsNullOrWhiteSpace(accountName)
            ? null
            : new OrganizationalIdentity(stableId, accountName);
    }
}
