namespace EosDashboards.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string password, string passwordHash);
}

public enum PasswordVerificationResult
{
    Failed,
    Succeeded,
    RehashNeeded,
}
