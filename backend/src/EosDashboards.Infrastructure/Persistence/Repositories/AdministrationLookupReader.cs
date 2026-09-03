using EosDashboards.Application.Administration;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class AdministrationLookupReader(EosDashboardDbContext context) : IAdministrationLookupReader
{
    private IQueryable<AdministrationUserListItem> Users => context.Users.AsNoTracking()
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName).ThenBy(user => user.Id)
            .Select(user => new AdministrationUserListItem(user.Id, user.OrganizationalId,
                user.FirstName, user.LastName, user.Username, user.MaskedMobileNumber, user.DepartmentId,
                context.Departments.Where(department => department.Id == user.DepartmentId).Select(department => department.Name).Single(),
                user.IsActive, user.MustChangePassword, user.UserRoles.Select(role => role.RoleId).ToArray(), user.RowVersion));

    public async Task<PagedResult<AdministrationUserListItem>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await Users.LongCountAsync(cancellationToken);
        var items = await Users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new PagedResult<AdministrationUserListItem>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<AdministrationUserListItem?> GetUserAsync(long id, CancellationToken cancellationToken)
    {
        var user = await (
            from candidate in context.Users.AsNoTracking()
            join department in context.Departments.AsNoTracking()
                on candidate.DepartmentId equals department.Id
            where candidate.Id == id
            select new
            {
                candidate.Id,
                PersonnelCode = candidate.OrganizationalId,
                candidate.FirstName,
                candidate.LastName,
                candidate.Username,
                MaskedMobile = candidate.MaskedMobileNumber,
                candidate.DepartmentId,
                DepartmentName = department.Name,
                candidate.IsActive,
                candidate.MustChangePassword,
                candidate.RowVersion
            }).SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleIds = await context.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == id)
            .OrderBy(userRole => userRole.RoleId)
            .Select(userRole => userRole.RoleId)
            .ToArrayAsync(cancellationToken);

        return new AdministrationUserListItem(
            user.Id,
            user.PersonnelCode,
            user.FirstName,
            user.LastName,
            user.Username,
            user.MaskedMobile,
            user.DepartmentId,
            user.DepartmentName,
            user.IsActive,
            user.MustChangePassword,
            roleIds,
            user.RowVersion);
    }

    public async Task<IReadOnlyList<AdministrationRoleListItem>> GetRolesAsync(CancellationToken cancellationToken) =>
        await context.Roles.AsNoTracking().Where(role => role.IsActive && role.IsSystem).OrderBy(role => role.Code)
            .Select(role => new AdministrationRoleListItem(role.Id, role.Code, role.DisplayName)).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentListItem>> GetDepartmentsAsync(CancellationToken cancellationToken) =>
        await context.Departments.AsNoTracking().OrderBy(department => department.Name)
            .Select(department => new DepartmentListItem(department.Id, department.Name, department.ParentDepartmentId, department.RowVersion))
            .ToArrayAsync(cancellationToken);
}
