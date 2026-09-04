using EosDashboards.Application.JobDescriptions;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionScopeReader(EosDashboardDbContext context) : IJobDescriptionScope
{
    public async Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken)
    {
        var departmentId = await context.Users
            .Where(user => user.Id == actorUserId && user.IsActive && user.UserRoles.Any(userRole =>
                context.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "DepartmentManager" && role.IsActive)))
            .Select(user => (long?)user.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (departmentId is null)
        {
            return [];
        }

        var childIds = await context.Departments
            .Where(department => department.ParentDepartmentId == departmentId.Value)
            .Select(department => department.Id)
            .ToArrayAsync(cancellationToken);
        return [departmentId.Value, .. childIds];
    }

    public async Task<bool> CanManageDepartmentAsync(
        long actorUserId,
        long departmentId,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || departmentId <= 0)
        {
            return false;
        }

        var managerRoleId = await context.Roles
            .Where(role => role.Code == "DepartmentManager" && role.IsActive)
            .Select(role => (long?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (managerRoleId is null)
        {
            return false;
        }

        return await context.Users.AnyAsync(user =>
            user.Id == actorUserId &&
            user.IsActive &&
            user.UserRoles.Any(userRole => userRole.RoleId == managerRoleId.Value) &&
            (user.DepartmentId == departmentId || context.Departments.Any(department =>
                department.Id == departmentId && department.ParentDepartmentId == user.DepartmentId)),
            cancellationToken);
    }

    public async Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken)
    {
        if (actorUserId <= 0)
        {
            return false;
        }

        var roleId = await context.Roles
            .Where(role => role.Code == "HumanResourcesManager" && role.IsActive)
            .Select(role => (long?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return roleId is not null && await context.Users.AnyAsync(user =>
            user.Id == actorUserId && user.IsActive &&
            user.UserRoles.Any(userRole => userRole.RoleId == roleId.Value),
            cancellationToken);
    }
}
