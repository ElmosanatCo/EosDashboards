namespace EosDashboards.IntegrationTests.Database;

public sealed class SqlServerDatabaseFixtureTests
{
    [Fact]
    public void RequireSafeConnectionString_RejectsDevelopmentCatalog()
    {
        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = "not-used",
            InitialCatalog = "EosDashboard",
            IntegratedSecurity = true,
        }.ConnectionString;

        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerDatabaseFixture.RequireSafeConnectionString(connectionString));

        Assert.Equal(
            "The SQL Server integration-test catalog must end with _IntegrationTests.",
            exception.Message);
    }

    [Fact]
    public void RequireSafeConnectionString_AcceptsDedicatedCatalog()
    {
        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = "not-used",
            InitialCatalog = "EosDashboard_IntegrationTests",
            IntegratedSecurity = true,
        }.ConnectionString;

        Assert.Same(
            connectionString,
            SqlServerDatabaseFixture.RequireSafeConnectionString(connectionString));
    }
}
