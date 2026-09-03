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
    public void Production_validation_rejects_a_token_from_a_different_issuer()
    {
        // Break caught: disabling issuer validation in the production token-validation policy.
        var signingKey = Enumerable.Repeat((byte)141, 32).ToArray();
        var expectedIssuer = CreateIssuer(signingKey);
        var differentIssuer = CreateIssuer(signingKey, issuerName: "Different.Issuer");
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-1);
        var token = differentIssuer.Issue(
            CreateUser(17, 31),
            23,
            issuedAtUtc,
            issuedAtUtc.AddMinutes(10));

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            Validate(token.Value, expectedIssuer.CreateValidationParameters()));
    }

    [Fact]
    public void Production_validation_rejects_a_token_for_a_different_audience()
    {
        // Break caught: disabling audience validation in the production token-validation policy.
        var signingKey = Enumerable.Repeat((byte)151, 32).ToArray();
        var expectedAudience = CreateIssuer(signingKey);
        var differentAudience = CreateIssuer(signingKey, audienceName: "Different.Audience");
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-1);
        var token = differentAudience.Issue(
            CreateUser(17, 31),
            23,
            issuedAtUtc,
            issuedAtUtc.AddMinutes(10));

        Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
            Validate(token.Value, expectedAudience.CreateValidationParameters()));
    }

    [Fact]
    public void Production_validation_rejects_a_token_signed_with_a_different_key()
    {
        // Break caught: bypassing signature validation in the production token-validation policy.
        var expectedIssuer = CreateIssuer(Enumerable.Repeat((byte)161, 32).ToArray());
        var differentSigner = CreateIssuer(Enumerable.Repeat((byte)162, 32).ToArray());
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-1);
        var token = differentSigner.Issue(
            CreateUser(17, 31),
            23,
            issuedAtUtc,
            issuedAtUtc.AddMinutes(10));

        Assert.ThrowsAny<SecurityTokenException>(() =>
            Validate(token.Value, expectedIssuer.CreateValidationParameters()));
    }

    [Fact]
    public void Production_validation_rejects_a_valid_signature_using_hs384()
    {
        // Break caught: removing the HS256 allowlist from the production validation policy.
        var signingKey = Enumerable.Repeat((byte)171, 64).ToArray();
        var issuer = CreateIssuer(signingKey);
        var issuedAtUtc = CurrentWholeSecond().AddMinutes(-1);
        var token = CreateToken(
            signingKey,
            SecurityAlgorithms.HmacSha384,
            issuedAtUtc,
            issuedAtUtc.AddMinutes(10));

        Assert.ThrowsAny<SecurityTokenException>(() =>
            Validate(token, issuer.CreateValidationParameters()));
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

    private static JwtAccessTokenIssuer CreateIssuer(
        byte[] signingKey,
        string issuerName = "EosDashboards.Tests",
        string audienceName = "EosDashboards.Tests.Client")
    {
        return new JwtAccessTokenIssuer(Options.Create(new AuthSecurityOptions
        {
            Issuer = issuerName,
            Audience = audienceName,
            SigningKey = Convert.ToBase64String(signingKey),
            AccessTokenLifetime = TimeSpan.FromMinutes(10),
            SessionLifetime = TimeSpan.FromHours(8),
        }));
    }

    private static string CreateToken(
        byte[] signingKey,
        string algorithm,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var token = new JwtSecurityToken(
            "EosDashboards.Tests",
            "EosDashboards.Tests.Client",
            [
                new Claim(JwtRegisteredClaimNames.Sub, "17"),
                new Claim(JwtRegisteredClaimNames.Sid, "23"),
            ],
            issuedAtUtc.UtcDateTime,
            expiresAtUtc.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(signingKey), algorithm));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void Validate(string token, TokenValidationParameters validationParameters)
    {
        new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(token, validationParameters, out _);
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
            1,
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
