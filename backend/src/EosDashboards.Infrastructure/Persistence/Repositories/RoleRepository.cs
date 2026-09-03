using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(EosDashboardDbContext context) : IRoleRepository
{
    public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Roles.AsNoTracking().SingleOrDefaultAsync(role => role.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.Roles
            .AsNoTracking()
            .Where(role => ids.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(Role role) => context.Roles.Add(role);
}
