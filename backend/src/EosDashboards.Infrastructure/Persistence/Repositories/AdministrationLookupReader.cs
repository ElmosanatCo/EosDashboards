using EosDashboards.Application.Administration;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class AdministrationLookupReader(EosDashboardDbContext context) : IAdministrationLookupReader
{
    private IQueryable<AdministrationUserListItem> Users => context.Users.AsNoTracking()
            .Select(user => new AdministrationUserListItem(user.Id, user.OrganizationalId, user.AccountName,
                user.FirstName, user.LastName, user.Username, user.MaskedMobileNumber, user.DepartmentId,
                context.Departments.Where(department => department.Id == user.DepartmentId).Select(department => department.Name).Single(),
                user.IsActive, user.MustChangePassword, user.UserRoles.Select(role => role.RoleId).ToArray(), user.RowVersion));

    public async Task<PagedResult<AdministrationUserListItem>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await Users.LongCountAsync(cancellationToken);
        var items = await Users.OrderBy(user => user.LastName).ThenBy(user => user.FirstName).ThenBy(user => user.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new PagedResult<AdministrationUserListItem>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<AdministrationUserListItem?> GetUserAsync(long id, CancellationToken cancellationToken) =>
        await Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AdministrationRoleListItem>> GetRolesAsync(CancellationToken cancellationToken) =>
        await context.Roles.AsNoTracking().Where(role => role.IsActive && role.IsSystem).OrderBy(role => role.Code)
            .Select(role => new AdministrationRoleListItem(role.Id, role.Code, role.DisplayName)).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentListItem>> GetDepartmentsAsync(CancellationToken cancellationToken) =>
        await context.Departments.AsNoTracking().OrderBy(department => department.Name)
            .Select(department => new DepartmentListItem(department.Id, department.Name, department.ParentDepartmentId, department.RowVersion))
            .ToArrayAsync(cancellationToken);
}
