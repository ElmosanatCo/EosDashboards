using EosDashboards.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EosDashboards.IntegrationTests.Database;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerDatabaseCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SQL Server database";
}

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public SqlServerDatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<SqlServerDatabaseFixture>(optional: true)
            .Build();

        _connectionString = RequireSafeConnectionString(
            configuration.GetConnectionString("EosDashboardTests"));
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();

        // Destructive setup is allowed only after RequireSafeConnectionString has accepted the catalog.
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public EosDashboardDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EosDashboardDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new EosDashboardDbContext(options);
    }

    internal static string RequireSafeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A SQL Server connection is required in ConnectionStrings:EosDashboardTests.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("The SQL Server integration-test connection is invalid.");
        }

        if (!builder.InitialCatalog.EndsWith("_IntegrationTests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The SQL Server integration-test catalog must end with _IntegrationTests.");
        }

        return connectionString;
    }
}
