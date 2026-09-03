using System.Security;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EosDashboards.Infrastructure.Security;

public sealed class AuthSecurityOptions
{
    public const string SectionName = "AuthSecurity";

    public string HashingKey { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan SessionLifetime { get; init; } = TimeSpan.FromHours(8);

    public string KeyRingPath { get; init; } = string.Empty;
}

internal sealed class AuthSecurityOptionsValidator : IValidateOptions<AuthSecurityOptions>
{
    private const int MinimumKeyByteCount = 32;
    private static readonly TimeSpan RequiredAccessTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RequiredSessionLifetime = TimeSpan.FromHours(8);

    public ValidateOptionsResult Validate(string? name, AuthSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        ValidateKey(options.HashingKey, nameof(AuthSecurityOptions.HashingKey), failures);
        ValidateKey(options.SigningKey, nameof(AuthSecurityOptions.SigningKey), failures);
        ValidateRequired(options.Issuer, nameof(AuthSecurityOptions.Issuer), failures);
        ValidateRequired(options.Audience, nameof(AuthSecurityOptions.Audience), failures);

        if (options.AccessTokenLifetime != RequiredAccessTokenLifetime)
        {
            failures.Add(Property(nameof(AuthSecurityOptions.AccessTokenLifetime)));
        }

        if (options.SessionLifetime != RequiredSessionLifetime)
        {
            failures.Add(Property(nameof(AuthSecurityOptions.SessionLifetime)));
        }

        if (!IsWritableDirectory(options.KeyRingPath))
        {
            failures.Add(Property(nameof(AuthSecurityOptions.KeyRingPath)));
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateKey(string encodedKey, string propertyName, ICollection<string> failures)
    {
        byte[]? key = null;
        try
        {
            key = Convert.FromBase64String(encodedKey);
            if (key.Length < MinimumKeyByteCount)
            {
                failures.Add(Property(propertyName));
            }
        }
        catch (FormatException)
        {
            failures.Add(Property(propertyName));
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    private static void ValidateRequired(string value, string propertyName, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(Property(propertyName));
        }
    }

    private static bool IsWritableDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string? probePath = null;
        try
        {
            var resolvedPath = Path.GetFullPath(path);
            Directory.CreateDirectory(resolvedPath);
            probePath = Path.Combine(resolvedPath, $".write-probe-{Guid.NewGuid():N}");
            using var probe = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.None);
            probe.WriteByte(0);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or UnauthorizedAccessException or
                NotSupportedException or SecurityException)
        {
            return false;
        }
        finally
        {
            if (probePath is not null)
            {
                try
                {
                    File.Delete(probePath);
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or SecurityException)
                {
                    // The probe is non-sensitive and uniquely named; startup validation remains safe.
                }
            }
        }
    }

    private static string Property(string propertyName)
    {
        return $"{nameof(AuthSecurityOptions)}.{propertyName}";
    }
}
