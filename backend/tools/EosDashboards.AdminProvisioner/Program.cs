using EosDashboards.AdminProvisioner;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Provisioning;
using EosDashboards.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.InputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(AdminProvisionerRunner).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

return await AdminProvisionerRunner.RunAsync(
    args,
    new SystemInteractiveConsole(),
    configuration,
    CancellationToken.None);

namespace EosDashboards.AdminProvisioner
{
    public static class AdminProvisionerComposition
    {
        public static IServiceCollection AddAdminProvisioner(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddInfrastructurePersistence(configuration);
            services.AddInfrastructureSecurity(configuration);
            services.AddSingleton<ICorrelationContext>(
                new ProvisioningCorrelationContext(Guid.NewGuid().ToString("N")));
            services.AddScoped<ProvisionSystemAdministrator>();
            return services;
        }
    }

    public static class AdminProvisionerRunner
    {
        public static async Task<int> RunAsync(
            string[] args,
            IInteractiveConsole console,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            if (args.Length != 0)
            {
                console.WriteLine("ورودی خط فرمان پذیرفته نیست.");
                return 2;
            }

            try
            {
                var services = new ServiceCollection();
                services.AddAdminProvisioner(configuration);
                await using var provider = services.BuildServiceProvider(
                    new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
                var mobileProtector = provider.GetRequiredService<IMobileProtector>();
                var command = new InteractiveInput(console, mobileProtector).Read();
                if (command is null)
                {
                    return 1;
                }

                await using var scope = provider.CreateAsyncScope();
                var result = await scope.ServiceProvider
                    .GetRequiredService<ProvisionSystemAdministrator>()
                    .HandleAsync(command, cancellationToken);
                console.WriteLine(
                    $"مدیر سامانه با شماره همراه {result.MaskedMobile} آماده است.");
                return 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                console.WriteLine("عملیات لغو شد.");
                return 1;
            }
            catch (Exception exception)
            {
                console.WriteLine("انجام عملیات ممکن نشد؛ پیکربندی امن و پایگاه داده را بررسی کنید.");
                if (string.Equals(
                        configuration["ProvisioningDiagnostics:ExposeFailureType"],
                        "true",
                        StringComparison.OrdinalIgnoreCase))
                {
                    console.WriteLine($"Diagnostic failure type: {exception.GetType().Name}.");
                }

                return 1;
            }
        }
    }

    internal sealed class ProvisioningCorrelationContext(string traceId) : ICorrelationContext
    {
        public string TraceId { get; } = traceId;
    }
}
