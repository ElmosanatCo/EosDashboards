namespace EosDashboards.Domain.Entities;

public sealed class UserPreference
{
    private UserPreference(
        long userId,
        string appearanceMode,
        string palette,
        bool sidebarCollapsed,
        DateTime createdAt)
    {
        UserId = userId;
        AppearanceMode = appearanceMode;
        Palette = palette;
        SidebarCollapsed = sidebarCollapsed;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public string AppearanceMode { get; private set; }

    public string Palette { get; private set; }

    public bool SidebarCollapsed { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static UserPreference Create(
        long userId,
        string appearanceMode,
        string palette,
        bool sidebarCollapsed,
        DateTime createdAt)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(appearanceMode))
        {
            throw new ArgumentException("An appearance mode is required.", nameof(appearanceMode));
        }

        if (string.IsNullOrWhiteSpace(palette))
        {
            throw new ArgumentException("A palette is required.", nameof(palette));
        }

        return new UserPreference(userId, appearanceMode, palette, sidebarCollapsed, createdAt);
    }

    public bool Update(
        string appearanceMode,
        string palette,
        bool sidebarCollapsed,
        DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(appearanceMode))
        {
            throw new ArgumentException("An appearance mode is required.", nameof(appearanceMode));
        }

        if (string.IsNullOrWhiteSpace(palette))
        {
            throw new ArgumentException("A palette is required.", nameof(palette));
        }

        if (AppearanceMode == appearanceMode &&
            Palette == palette &&
            SidebarCollapsed == sidebarCollapsed)
        {
            return false;
        }

        AppearanceMode = appearanceMode;
        Palette = palette;
        SidebarCollapsed = sidebarCollapsed;
        UpdatedAt = updatedAt;
        return true;
    }
}
