using EosDashboards.Application.Abstractions;
using EosDashboards.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EosDashboards.IntegrationTests.Database;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersEveryPersistencePortAsScoped()
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "not-used",
            InitialCatalog = "EosDashboard_IntegrationTests",
            IntegratedSecurity = true,
        }.ConnectionString;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EosDashboard"] = connectionString,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        var persistencePorts = new[]
        {
            typeof(IUserRepository),
            typeof(IRoleRepository),
            typeof(IOtpChallengeRepository),
            typeof(IUserSessionRepository),
            typeof(IUserPreferenceRepository),
            typeof(IAuditWriter),
            typeof(IUnitOfWork),
        };
        Assert.All(
            persistencePorts,
            port => Assert.Contains(
                services,
                descriptor => descriptor.ServiceType == port &&
                              descriptor.Lifetime == ServiceLifetime.Scoped));
    }
}
