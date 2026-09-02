using EosDashboards.Application.Abstractions;
using EosDashboards.Infrastructure.Persistence;
using EosDashboards.Infrastructure.Persistence.Repositories;
using EosDashboards.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EosDashboards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EosDashboard");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A database connection is required in ConnectionStrings:EosDashboard.");
        }

        services.AddDbContext<EosDashboardDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddOptions<AuthSecurityOptions>()
            .Bind(configuration.GetSection(AuthSecurityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AuthSecurityOptions>, AuthSecurityOptionsValidator>();
        services.AddSingleton<IDataProtectionProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AuthSecurityOptions>>().Value;
            return DataProtectionProvider.Create(
                new DirectoryInfo(options.KeyRingPath),
                builder => builder.SetApplicationName("EosDashboards"));
        });
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISecretHasher, HmacSecretHasher>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IMobileProtector, DataProtectionMobileProtector>();
        services.AddSingleton<JwtAccessTokenIssuer>();
        services.AddSingleton<IAccessTokenIssuer>(serviceProvider =>
            serviceProvider.GetRequiredService<JwtAccessTokenIssuer>());
        services.AddSingleton<TokenValidationParameters>(serviceProvider =>
            serviceProvider.GetRequiredService<JwtAccessTokenIssuer>().CreateValidationParameters());

        return services;
    }
}
