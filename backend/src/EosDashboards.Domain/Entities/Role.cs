namespace EosDashboards.Domain.Entities;

public sealed class Role
{
    private Role(string code, string displayName, bool isSystem, DateTime createdAt)
    {
        Code = code;
        DisplayName = displayName;
        IsSystem = isSystem;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }

    public string Code { get; private set; }

    public string DisplayName { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Role Create(string code, string displayName, bool isSystem, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A role code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A role display name is required.", nameof(displayName));
        }

        return new Role(code, displayName, isSystem, createdAt);
    }
}
