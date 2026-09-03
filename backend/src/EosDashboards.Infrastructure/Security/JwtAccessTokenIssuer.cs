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
        DateTime issuedAt,
        DateTime expiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessionId);

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Token expiry must follow issue time.");
        }

        var protocolIssuedAt = new DateTimeOffset(
            DateTime.SpecifyKind(issuedAt, DateTimeKind.Local));
        var protocolExpiresAt = new DateTimeOffset(
            DateTime.SpecifyKind(expiresAt, DateTimeKind.Local));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString(CultureInfo.InvariantCulture)),
            new(
                JwtRegisteredClaimNames.Iat,
                protocolIssuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
        };
        claims.AddRange(user.UserRoles.Select(userRole =>
            new Claim("role", userRole.RoleId.ToString(CultureInfo.InvariantCulture))));

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            protocolIssuedAt.UtcDateTime,
            protocolExpiresAt.UtcDateTime,
            new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));
        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAccessToken(value, expiresAt);
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
