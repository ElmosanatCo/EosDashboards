using EosDashboards.Application.Auth;

namespace EosDashboards.Application.Tests.Auth;

public sealed class AuthContractSafetyTests
{
    [Fact]
    public void Sensitive_contract_diagnostic_text_omits_credentials_codes_tokens_and_mobile()
    {
        // Break caught: accidental logging of authentication secrets through record ToString implementations.
        const string secret = "sensitive-value";
        var expiresAt = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Unspecified);
        object[] contracts =
        [
            new StartSignInCommand(secret, secret, secret),
            new VerifyOtpCommand(secret, secret, secret),
            new StartPasswordResetCommand(secret, secret),
            new CompletePasswordResetCommand(secret, secret, secret, secret),
            new ChangePasswordCommand(11, secret, secret),
            new RefreshSessionCommand(secret),
            new SmsMessage(secret, secret),
            new IssuedAccessToken(secret, expiresAt),
            new StartSignInResult(StartSignInStatus.Succeeded, secret, secret, expiresAt, expiresAt),
            new AuthenticatedUser(11, secret, secret, secret, [31], ["SystemAdministrator"], new AuthenticatedDepartment(1, "واحد آزمایشی"), false),
            new AuthenticationResult(
                VerifyOtpStatus.Succeeded,
                new IssuedAccessToken(secret, expiresAt),
                secret,
                expiresAt,
                null),
            new RefreshSessionResult(
                RefreshSessionStatus.Succeeded,
                new IssuedAccessToken(secret, expiresAt),
                secret,
                expiresAt,
                null),
        ];

        Assert.All(
            contracts,
            contract => Assert.DoesNotContain(secret, contract.ToString(), StringComparison.Ordinal));
    }
}
