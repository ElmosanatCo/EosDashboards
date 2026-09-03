using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(EosDashboardDbContext context) : IUserRepository
{
    public Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken) =>
        context.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.OrganizationalId == stableId, cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        context.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetForUpdateAsync(long id, CancellationToken cancellationToken) =>
        context.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<int> CountActiveWithRoleAsync(long roleId, CancellationToken cancellationToken) =>
        context.Users.CountAsync(
            user => user.IsActive && user.UserRoles.Any(userRole => userRole.RoleId == roleId),
            cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
