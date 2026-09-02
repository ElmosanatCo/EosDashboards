using EosDashboards.Application.Abstractions;
using EosDashboards.Infrastructure;
using EosDashboards.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EosDashboards.IntegrationTests.Security;

public sealed class AuthSecurityOptionsTests
{
    [Fact]
    public async Task Valid_options_start_and_resolve_every_security_port_with_persisted_protection_keys()
    {
        // Break caught: incomplete DI wiring, deferred invalid options, or ephemeral mobile protection keys.
        using var keyRing = new TemporaryDirectory();
        using var host = BuildHost(ValidConfiguration(keyRing.Path));

        await host.StartAsync();

        Assert.IsType<SystemClock>(host.Services.GetRequiredService<IClock>());
        Assert.IsType<HmacSecretHasher>(host.Services.GetRequiredService<ISecretHasher>());
        Assert.IsType<SecureTokenGenerator>(host.Services.GetRequiredService<ISecureTokenGenerator>());
        var mobileProtector = Assert.IsType<DataProtectionMobileProtector>(
            host.Services.GetRequiredService<IMobileProtector>());
        Assert.IsType<JwtAccessTokenIssuer>(host.Services.GetRequiredService<IAccessTokenIssuer>());
        Assert.Equal(
            TimeSpan.Zero,
            host.Services.GetRequiredService<TokenValidationParameters>().ClockSkew);

        mobileProtector.Protect("09123456789");
        Assert.NotEmpty(Directory.EnumerateFiles(keyRing.Path));
    }

    [Theory]
    [InlineData("HashingKey", "not-base64")]
    [InlineData("HashingKey", "AQID")]
    [InlineData("SigningKey", "not-base64")]
    [InlineData("SigningKey", "AQID")]
    [InlineData("Issuer", " ")]
    [InlineData("Audience", " ")]
    [InlineData("AccessTokenLifetime", "00:09:59")]
    [InlineData("SessionLifetime", "07:59:59")]
    public async Task Invalid_options_fail_startup_with_only_the_option_property(
        string propertyName,
        string invalidValue)
    {
        // Break caught: accepting weak/incorrect security configuration or reporting its supplied value.
        using var keyRing = new TemporaryDirectory();
        var configuration = ValidConfiguration(keyRing.Path);
        configuration[$"{AuthSecurityOptions.SectionName}:{propertyName}"] = invalidValue;
        using var host = BuildHost(configuration);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Equal([$"{nameof(AuthSecurityOptions)}.{propertyName}"], exception.Failures);
        Assert.DoesNotContain(invalidValue, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_directory_key_ring_path_fails_startup_without_echoing_the_path()
    {
        // Break caught: starting with an unusable persistent key-ring target or leaking its location.
        using var testDirectory = new TemporaryDirectory();
        var filePath = System.IO.Path.Combine(testDirectory.Path, "existing-file");
        File.WriteAllText(filePath, "synthetic");
        var configuration = ValidConfiguration(filePath);
        using var host = BuildHost(configuration);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Equal(
            [$"{nameof(AuthSecurityOptions)}.{nameof(AuthSecurityOptions.KeyRingPath)}"],
            exception.Failures);
        Assert.DoesNotContain(filePath, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IHost BuildHost(Dictionary<string, string?> configuration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder.Build();
    }

    private static Dictionary<string, string?> ValidConfiguration(string keyRingPath)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "not-used",
            InitialCatalog = "EosDashboard_IntegrationTests",
            IntegratedSecurity = true,
        }.ConnectionString;
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:EosDashboard"] = connectionString,
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.HashingKey)}"] =
                Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.SigningKey)}"] =
                Convert.ToBase64String(Enumerable.Range(33, 32).Select(value => (byte)value).ToArray()),
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.Issuer)}"] =
                "EosDashboards.Tests",
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.Audience)}"] =
                "EosDashboards.Tests.Client",
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.AccessTokenLifetime)}"] =
                "00:10:00",
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.SessionLifetime)}"] =
                "08:00:00",
            [$"{AuthSecurityOptions.SectionName}:{nameof(AuthSecurityOptions.KeyRingPath)}"] = keyRingPath,
        };
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private static readonly string TestRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EosDashboards.Tests"));
        private readonly string _path;

        public TemporaryDirectory()
        {
            _path = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(TestRoot, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(_path);
        }

        public string Path => _path;

        public void Dispose()
        {
            var resolvedPath = System.IO.Path.GetFullPath(_path);
            var expectedPrefix = TestRoot + System.IO.Path.DirectorySeparatorChar;
            if (!resolvedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    Directory.GetParent(resolvedPath)?.FullName,
                    TestRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Temporary path is outside the test root.");
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
            }
        }
    }
}
