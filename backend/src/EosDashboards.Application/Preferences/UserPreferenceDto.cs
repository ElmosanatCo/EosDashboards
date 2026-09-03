namespace EosDashboards.Application.Preferences;

public sealed record UserPreferenceDto(
    string AppearanceMode,
    string Palette,
    bool SidebarCollapsed);
