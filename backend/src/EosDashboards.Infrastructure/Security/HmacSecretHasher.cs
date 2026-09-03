using System.Security.Cryptography;
using System.Text;
using EosDashboards.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EosDashboards.Infrastructure.Security;

public sealed class HmacSecretHasher : ISecretHasher
{
    private const int Sha256ByteCount = 32;
    private readonly byte[] _key;

    public HmacSecretHasher(IOptions<AuthSecurityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _key = Convert.FromBase64String(options.Value.HashingKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{nameof(AuthSecurityOptions.HashingKey)} is invalid.",
                exception);
        }
    }

    public string Hash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("A value is required.", nameof(value));
        }

        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    public bool Verify(string value, string expectedHash)
    {
        if (string.IsNullOrEmpty(value) ||
            string.IsNullOrEmpty(expectedHash) ||
            expectedHash.Length != Sha256ByteCount * 2)
        {
            return false;
        }

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualBytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
