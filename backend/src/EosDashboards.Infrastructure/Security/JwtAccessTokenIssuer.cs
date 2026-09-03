using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EosDashboards.Infrastructure.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtAccessTokenIssuer(IOptions<AuthSecurityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _issuer = options.Value.Issuer;
        _audience = options.Value.Audience;
        _signingKey = new SymmetricSecurityKey(DecodeSigningKey(options.Value.SigningKey));
    }

    public IssuedAccessToken Issue(
        User user,
        long sessionId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessionId);

        var normalizedIssuedAtUtc = issuedAtUtc.ToUniversalTime();
        var normalizedExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        if (normalizedExpiresAtUtc <= normalizedIssuedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "Token expiry must follow issue time.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString(CultureInfo.InvariantCulture)),
            new(
                JwtRegisteredClaimNames.Iat,
                normalizedIssuedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
        };
        claims.AddRange(user.UserRoles.Select(userRole =>
            new Claim("role", userRole.RoleId.ToString(CultureInfo.InvariantCulture))));

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            normalizedIssuedAtUtc.UtcDateTime,
            normalizedExpiresAtUtc.UtcDateTime,
            new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));
        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAccessToken(value, normalizedExpiresAtUtc);
    }

    public TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = _audience,
            ValidIssuer = _issuer,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = _signingKey,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
        };
    }

    private static byte[] DecodeSigningKey(string encodedKey)
    {
        try
        {
            return Convert.FromBase64String(encodedKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{nameof(AuthSecurityOptions.SigningKey)} is invalid.",
                exception);
        }
    }
}
