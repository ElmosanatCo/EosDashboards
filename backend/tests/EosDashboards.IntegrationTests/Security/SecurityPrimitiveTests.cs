using System.Text.RegularExpressions;
using EosDashboards.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace EosDashboards.IntegrationTests.Security;

public sealed partial class SecurityPrimitiveTests
{
    [Fact]
    public void Six_digit_codes_always_use_exactly_six_ascii_digits()
    {
        // Break caught: biased or variably formatted OTP generation, including lost leading zeroes.
        var generator = new SecureTokenGenerator();

        var codes = Enumerable.Range(0, 10_000).Select(_ => generator.CreateSixDigitCode());

        Assert.All(codes, code => Assert.Matches(SixAsciiDigits(), code));
    }

    [Fact]
    public void Opaque_tokens_are_unpadded_base64url_with_the_requested_entropy_size()
    {
        // Break caught: emitting fewer random bytes or standard Base64 characters unsafe for URLs.
        var generator = new SecureTokenGenerator();

        var token = generator.CreateOpaqueToken(32);

        Assert.Matches(Base64Url(), token);
        Assert.DoesNotContain('=', token);
        Assert.Equal(32, DecodeBase64Url(token).Length);
    }

    [Fact]
    public void Opaque_token_generation_rejects_nonpositive_sizes_without_sensitive_text()
    {
        // Break caught: silently producing an empty or invalid security credential.
        var generator = new SecureTokenGenerator();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => generator.CreateOpaqueToken(0));

        Assert.Equal("byteCount", exception.ParamName);
    }

    [Fact]
    public void Hmac_hashing_is_deterministic_keyed_uppercase_sha256_hex()
    {
        // Break caught: changing the keyed algorithm, encoding, or persisted hash representation.
        var hasher = CreateHasher(1);

        var first = hasher.Hash("otp-value");
        var second = hasher.Hash("otp-value");

        Assert.Equal("453D2E15B85D238D90FF25D3CC87B6BA219482A1C5F92617FC9E3C031774E513", first);
        Assert.Equal(first, second);
        Assert.Matches(UppercaseSha256Hex(), first);
    }

    [Fact]
    public void Hmac_hashes_change_with_input_and_key_and_verify_only_exact_matches()
    {
        // Break caught: unkeyed hashing or verification that accepts a different input/key.
        var firstKey = CreateHasher(1);
        var secondKey = CreateHasher(2);
        var expected = firstKey.Hash("first-value");

        Assert.True(firstKey.Verify("first-value", expected));
        Assert.False(firstKey.Verify("second-value", expected));
        Assert.NotEqual(expected, secondKey.Hash("first-value"));
    }

    [Theory]
    [InlineData("", "00")]
    [InlineData("value", "")]
    [InlineData("value", "not-hex")]
    [InlineData("value", "AA")]
    public void Hmac_verification_rejects_empty_or_malformed_values_without_throwing(
        string value,
        string expectedHash)
    {
        // Break caught: parsing failures escaping the verification boundary or accepting truncated hashes.
        var hasher = CreateHasher(1);

        Assert.False(hasher.Verify(value, expectedHash));
    }

    [Fact]
    public void Hmac_hashing_rejects_empty_input_with_only_the_parameter_name()
    {
        // Break caught: assigning a stable valid hash to an absent credential.
        var hasher = CreateHasher(1);

        var exception = Assert.Throws<ArgumentException>(() => hasher.Hash(string.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void System_clock_returns_a_utc_instant()
    {
        // Break caught: returning local wall-clock time through the UTC application port.
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;

        var actual = clock.UtcNow;

        Assert.Equal(TimeSpan.Zero, actual.Offset);
        Assert.InRange(actual, before, DateTimeOffset.UtcNow);
    }

    private static HmacSecretHasher CreateHasher(byte firstByte)
    {
        var key = Enumerable.Range(firstByte, 32).Select(value => (byte)value).ToArray();
        var options = Options.Create(new AuthSecurityOptions
        {
            HashingKey = Convert.ToBase64String(key),
        });
        return new HmacSecretHasher(options);
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var standard = value.Replace('-', '+').Replace('_', '/');
        standard = standard.PadRight(standard.Length + ((4 - (standard.Length % 4)) % 4), '=');
        return Convert.FromBase64String(standard);
    }

    [GeneratedRegex("^[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex SixAsciiDigits();

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Base64Url();

    [GeneratedRegex("^[0-9A-F]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex UppercaseSha256Hex();
}
