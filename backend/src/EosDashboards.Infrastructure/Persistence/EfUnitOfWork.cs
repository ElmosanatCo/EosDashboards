using EosDashboards.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EosDashboards.Infrastructure.Persistence;

public sealed class EfUnitOfWork(EosDashboardDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteSerializedTransactionAsync(
        string operationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        ArgumentNullException.ThrowIfNull(operation);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        var result = new SqlParameter("@result", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output,
        };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = operationKey,
        };
        var lockTimeout = new SqlParameter("@lockTimeout", SqlDbType.Int)
        {
            Value = 30_000,
        };
        await context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sys.sp_getapplock @Resource = @resource, " +
            "@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @lockTimeout;",
            [result, resource, lockTimeout],
            cancellationToken);
        if (result.Value is not int lockResult || lockResult < 0)
        {
            throw new InvalidOperationException("The serialized transaction lock could not be acquired.");
        }

        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
