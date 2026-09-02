namespace EosDashboards.Application.Auth;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static bool IsValid(string? password) =>
        password is not null &&
        password.Length >= MinimumLength &&
        password.Length <= MaximumLength;
}
