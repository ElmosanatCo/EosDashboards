using EosDashboards.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace EosDashboards.IntegrationTests.Security;

public sealed class MobileProtectionTests
{
    [Fact]
    public void Mobile_protection_round_trips_with_the_versioned_purpose_without_plaintext_ciphertext()
    {
        // Break caught: changing the purpose string or persisting plaintext mobile data.
        using var keyRing = new TemporaryKeyRing();
        var provider = DataProtectionProvider.Create(keyRing.Path);
        var protector = new DataProtectionMobileProtector(provider);
        const string mobile = "09123456789";

        var protectedMobile = protector.Protect(mobile);

        Assert.DoesNotContain(mobile, protectedMobile, StringComparison.Ordinal);
        Assert.Equal(mobile, protector.Unprotect(protectedMobile));
        Assert.Equal(
            mobile,
            provider.CreateProtector("EosDashboards.Mobile.v1").Unprotect(protectedMobile));
    }

    [Fact]
    public void Mobile_protection_uses_a_privacy_conservative_fixed_width_mask()
    {
        // Break caught: exposing more than the final four digits or changing presentation width.
        using var keyRing = new TemporaryKeyRing();
        var protector = new DataProtectionMobileProtector(DataProtectionProvider.Create(keyRing.Path));

        var masked = protector.Mask("09123456789");

        Assert.Equal("*******6789", masked);
        Assert.Equal(11, masked.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("9123456789")]
    [InlineData("08123456789")]
    [InlineData("0912345678A")]
    [InlineData("091234567890")]
    public void Mobile_protection_rejects_non_normalized_iranian_shapes_without_echoing_input(string invalidMobile)
    {
        // Break caught: accepting non-normalized or non-Iranian mobile values at the protection boundary.
        using var keyRing = new TemporaryKeyRing();
        var protector = new DataProtectionMobileProtector(DataProtectionProvider.Create(keyRing.Path));

        var protectException = Assert.Throws<ArgumentException>(() => protector.Protect(invalidMobile));
        var maskException = Assert.Throws<ArgumentException>(() => protector.Mask(invalidMobile));

        Assert.Equal("normalizedMobile", protectException.ParamName);
        Assert.Equal("normalizedMobile", maskException.ParamName);
        if (invalidMobile.Length > 0)
        {
            Assert.DoesNotContain(invalidMobile, protectException.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(invalidMobile, maskException.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Mobile_unprotection_with_different_key_material_fails_without_exposing_values()
    {
        // Break caught: treating unauthenticated ciphertext as valid or leaking it through an error.
        using var firstKeyRing = new TemporaryKeyRing();
        using var secondKeyRing = new TemporaryKeyRing();
        var first = new DataProtectionMobileProtector(DataProtectionProvider.Create(firstKeyRing.Path));
        var second = new DataProtectionMobileProtector(DataProtectionProvider.Create(secondKeyRing.Path));
        const string mobile = "09123456789";
        var protectedMobile = first.Protect(mobile);

        var exception = Assert.Throws<InvalidOperationException>(() => second.Unprotect(protectedMobile));

        Assert.DoesNotContain(mobile, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(protectedMobile, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_unprotection_rejects_ciphertext_from_another_purpose_in_the_same_key_ring()
    {
        // Break caught: weakening purpose isolation or exposing protected/plaintext values on failure.
        using var keyRing = new TemporaryKeyRing();
        var provider = DataProtectionProvider.Create(keyRing.Path);
        var protector = new DataProtectionMobileProtector(provider);
        const string mobile = "09123456789";
        var wrongPurposeCiphertext = provider
            .CreateProtector("EosDashboards.Mobile.Other")
            .Protect(mobile);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            protector.Unprotect(wrongPurposeCiphertext));

        Assert.DoesNotContain(mobile, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(wrongPurposeCiphertext, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_protection_survives_provider_disposal_and_recreation_with_the_same_key_ring()
    {
        // Break caught: using process-only keys or instance-specific purpose material.
        using var keyRing = new TemporaryKeyRing();
        const string mobile = "09123456789";
        string protectedMobile;
        using (var servicesA = CreateDataProtectionServices(keyRing.Path))
        {
            var providerA = servicesA.GetRequiredService<IDataProtectionProvider>();
            var protectorA = new DataProtectionMobileProtector(providerA);
            protectedMobile = protectorA.Protect(mobile);
        }

        using (var servicesB = CreateDataProtectionServices(keyRing.Path))
        {
            var providerB = servicesB.GetRequiredService<IDataProtectionProvider>();
            var protectorB = new DataProtectionMobileProtector(providerB);
            Assert.Equal(mobile, protectorB.Unprotect(protectedMobile));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("malformed-protected-value")]
    public void Mobile_unprotection_rejects_empty_or_malformed_values_without_echoing_input(string invalidValue)
    {
        // Break caught: allowing malformed protected data to escape as plaintext or enter diagnostics.
        using var keyRing = new TemporaryKeyRing();
        var protector = new DataProtectionMobileProtector(DataProtectionProvider.Create(keyRing.Path));

        var exception = Assert.Throws<InvalidOperationException>(() => protector.Unprotect(invalidValue));

        if (invalidValue.Length > 0)
        {
            Assert.DoesNotContain(invalidValue, exception.Message, StringComparison.Ordinal);
        }
    }

    private static ServiceProvider CreateDataProtectionServices(string keyRingPath)
    {
        var services = new ServiceCollection();
        services
            .AddDataProtection()
            .SetApplicationName("EosDashboards")
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        return services.BuildServiceProvider();
    }

    private sealed class TemporaryKeyRing : IDisposable
    {
        private static readonly string TestRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EosDashboards.Tests"));
        private readonly string _path;

        public TemporaryKeyRing()
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
                throw new InvalidOperationException("Temporary key-ring path is outside the test root.");
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
            }
        }
    }
}
