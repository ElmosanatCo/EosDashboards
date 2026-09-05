using EosDashboards.Application.Abstractions;

namespace EosDashboards.Application.Preferences;

public sealed class GetMyPreferences(IUserPreferenceRepository preferences)
{
    public const string DefaultAppearanceMode = "dark";
    public const string DefaultPalette = UserPreferencePalettes.Teal;

    public async Task<UserPreferenceDto> HandleAsync(long userId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        var preference = await preferences.FindByUserIdAsync(userId, cancellationToken);
        return preference is null
            ? new UserPreferenceDto(DefaultAppearanceMode, DefaultPalette, false, true)
            : new UserPreferenceDto(
                preference.AppearanceMode,
                preference.Palette,
                preference.SidebarCollapsed,
                preference.GradientsEnabled);
    }
}
