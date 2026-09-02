namespace EosDashboards.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task ExecuteSerializedTransactionAsync(
        string operationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
