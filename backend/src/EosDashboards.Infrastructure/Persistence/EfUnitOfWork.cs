using EosDashboards.Application.Abstractions;

namespace EosDashboards.Infrastructure.Persistence;

public sealed class EfUnitOfWork(EosDashboardDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
