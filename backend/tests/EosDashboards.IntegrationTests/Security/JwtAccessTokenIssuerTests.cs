using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using EosDashboards.Domain.Entities;
using EosDashboards.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EosDashboards.IntegrationTests.Security;

public sealed class JwtAccessTokenIssuerTests
{
    [Fact]
    public void Issued_token_validates_issuer_audience_signature_lifetime_session_and_numeric_roles()
    {
        // Break caught: omitting a required identity/session claim or issuing an unverifiable token.
        var signingKey = Enumerable.Range(65, 32).Select(value => (byte)value).ToArray();
        var issuer = CreateIssuer(signingKey);
        var user = CreateUser(17, 31, 42);
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-1);
        var expiresAtUtc = issuedAtUtc.AddMinutes(10);

        var issued = issuer.Issue(user, 23, issuedAtUtc, expiresAtUtc);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            issued.Value,
            ExpectedValidationParameters(signingKey),
            out var validatedToken);

        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal("EosDashboards.Tests", jwt.Issuer);
        Assert.Equal(["EosDashboards.Tests.Client"], jwt.Audiences);
        Assert.Equal("17", principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal("23", principal.FindFirstValue(JwtRegisteredClaimNames.Sid));
        Assert.Equal(["31", "42"], principal.FindAll("role").Select(claim => claim.Value));
        Assert.Equal(expiresAtUtc, issued.ExpiresAtUtc);
        Assert.Equal(expiresAtUtc.UtcDateTime, jwt.ValidTo);
        Assert.Equal(issuedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            principal.FindFirstValue(JwtRegisteredClaimNames.Iat));
    }

    [Fact]
    public void Issuer_honors_an_explicit_shorter_final_session_expiry()
    {
        // Break caught: replacing the caller's absolute-session cap with a fresh ten-minute lifetime.
        var issuer = CreateIssuer(Enumerable.Repeat((byte)91, 32).ToArray());
        var user = CreateUser(17, 31);
        var issuedAtUtc = CurrentWholeSecond();
        var finalSessionExpiryUtc = issuedAtUtc.AddSeconds(45);

        var issued = issuer.Issue(user, 23, issuedAtUtc, finalSessionExpiryUtc);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Value);

        Assert.Equal(finalSessionExpiryUtc, issued.ExpiresAtUtc);
        Assert.Equal(finalSessionExpiryUtc.UtcDateTime, token.ValidTo);
    }

    [Fact]
    public void Validation_parameters_use_zero_clock_skew_and_reject_an_expired_token()
    {
        // Break caught: accepting an access token after its explicit expiry through default skew.
        var issuer = CreateIssuer(Enumerable.Repeat((byte)123, 32).ToArray());
        var validation = issuer.CreateValidationParameters();
        var user = CreateUser(17, 31);
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-2);
        var expiredAtUtc = CurrentWholeSecond().AddSeconds(-5);
        var issued = issuer.Issue(user, 23, issuedAtUtc, expiredAtUtc);

        Assert.Equal(TimeSpan.Zero, validation.ClockSkew);
        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(issued.Value, validation, out _));
    }

    [Fact]
    public void Issuer_rejects_nonpositive_session_ids_without_issuing_a_token()
    {
        // Break caught: issuing a token without a usable revocable-session identifier.
        var issuer = CreateIssuer(Enumerable.Repeat((byte)222, 32).ToArray());
        var user = CreateUser(17, 31);
        var issuedAtUtc = CurrentWholeSecond();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            issuer.Issue(user, 0, issuedAtUtc, issuedAtUtc.AddMinutes(1)));

        Assert.Equal("sessionId", exception.ParamName);
    }

    [Fact]
    public void Issuer_rejects_expiry_not_after_issue_time_without_issuing_a_token()
    {
        // Break caught: creating zero-length or backwards-lifetime credentials.
        var issuer = CreateIssuer(Enumerable.Repeat((byte)177, 32).ToArray());
        var user = CreateUser(17, 31);
        var issuedAtUtc = CurrentWholeSecond();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            issuer.Issue(user, 23, issuedAtUtc, issuedAtUtc));

        Assert.Equal("expiresAtUtc", exception.ParamName);
    }

    private static JwtAccessTokenIssuer CreateIssuer(byte[] signingKey)
    {
        return new JwtAccessTokenIssuer(Options.Create(new AuthSecurityOptions
        {
            Issuer = "EosDashboards.Tests",
            Audience = "EosDashboards.Tests.Client",
            SigningKey = Convert.ToBase64String(signingKey),
            AccessTokenLifetime = TimeSpan.FromMinutes(10),
            SessionLifetime = TimeSpan.FromHours(8),
        }));
    }

    private static TokenValidationParameters ExpectedValidationParameters(byte[] signingKey)
    {
        return new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = "EosDashboards.Tests.Client",
            ValidIssuer = "EosDashboards.Tests",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
    }

    private static User CreateUser(long userId, params long[] roleIds)
    {
        var user = User.Create(
            "synthetic-user",
            "TEST\\synthetic-user",
            "Synthetic",
            "User",
            "protected-value",
            "*******6789",
            DateTimeOffset.UtcNow.AddDays(-1));
        typeof(User)
            .GetProperty(nameof(User.Id), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(user, userId);
        foreach (var roleId in roleIds)
        {
            user.AssignRole(roleId);
        }

        return user;
    }

    private static DateTimeOffset CurrentWholeSecond()
    {
        return DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
