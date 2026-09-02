using EosDashboards.Application.Abstractions;
using EosDashboards.Infrastructure.Persistence;
using EosDashboards.Infrastructure.Persistence.Repositories;
using EosDashboards.Infrastructure.Security;
using EosDashboards.Infrastructure.Sms;
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
        services.AddInfrastructurePersistence(configuration);
        services.AddInfrastructureSecurity(configuration);
        services.AddInfrastructureSms(configuration);
        return services;
    }

    public static IServiceCollection AddInfrastructurePersistence(
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
        return services;
    }

    public static IServiceCollection AddInfrastructureSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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

    public static IServiceCollection AddInfrastructureSms(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SmsOptions>()
            .Bind(configuration.GetSection(SmsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SmsOptions>, SmsOptionsValidator>();
        services.AddHttpClient(SmsOptions.HttpClientName, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SmsOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint!, UriKind.Absolute);
            client.Timeout = options.Timeout;
        });
        services.AddScoped<ISmsSender>(serviceProvider => new SoapSmsSender(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(SmsOptions.HttpClientName)));

        return services;
    }
}
