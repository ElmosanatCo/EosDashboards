using System.Security.Cryptography;
using EosDashboards.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace EosDashboards.Infrastructure.Security;

public sealed class DataProtectionMobileProtector : IMobileProtector
{
    private const string Purpose = "EosDashboards.Mobile.v1";
    private const int NormalizedMobileLength = 11;
    private const int VisibleSuffixLength = 4;
    private readonly IDataProtector _protector;

    public DataProtectionMobileProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string normalizedMobile)
    {
        ValidateNormalizedMobile(normalizedMobile);
        return _protector.Protect(normalizedMobile);
    }

    public string Unprotect(string protectedMobile)
    {
        if (string.IsNullOrWhiteSpace(protectedMobile))
        {
            throw UnprotectFailure();
        }

        try
        {
            var normalizedMobile = _protector.Unprotect(protectedMobile);
            ValidateNormalizedMobile(normalizedMobile);
            return normalizedMobile;
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException or ArgumentException)
        {
            throw UnprotectFailure();
        }
    }

    public string Mask(string normalizedMobile)
    {
        ValidateNormalizedMobile(normalizedMobile);
        return new string('*', NormalizedMobileLength - VisibleSuffixLength) +
               normalizedMobile[^VisibleSuffixLength..];
    }

    private static void ValidateNormalizedMobile(string normalizedMobile)
    {
        if (normalizedMobile is null ||
            normalizedMobile.Length != NormalizedMobileLength ||
            normalizedMobile[0] != '0' ||
            normalizedMobile[1] != '9' ||
            normalizedMobile.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException(
                "A normalized Iranian mobile number is required.",
                nameof(normalizedMobile));
        }
    }

    private static InvalidOperationException UnprotectFailure()
    {
        return new InvalidOperationException("Protected mobile cannot be read.");
    }
}
