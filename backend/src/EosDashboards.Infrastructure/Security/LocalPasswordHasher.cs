using EosDashboards.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using ApplicationPasswordVerificationResult = EosDashboards.Application.Abstractions.PasswordVerificationResult;

namespace EosDashboards.Infrastructure.Security;

public sealed class LocalPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return _hasher.HashPassword(new object(), password);
    }

    public ApplicationPasswordVerificationResult Verify(string password, string passwordHash)
    {
        if (password is null || string.IsNullOrWhiteSpace(passwordHash))
        {
            return ApplicationPasswordVerificationResult.Failed;
        }

        return _hasher.VerifyHashedPassword(new object(), passwordHash, password) switch
        {
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success =>
                ApplicationPasswordVerificationResult.Succeeded,
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded =>
                ApplicationPasswordVerificationResult.RehashNeeded,
            _ => ApplicationPasswordVerificationResult.Failed,
        };
    }
}
