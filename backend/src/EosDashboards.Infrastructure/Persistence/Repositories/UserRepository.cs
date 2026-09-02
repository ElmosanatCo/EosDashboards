using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(EosDashboardDbContext context) : IUserRepository
{
    public Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.OrganizationalId == stableId, cancellationToken);

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
