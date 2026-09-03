using System.Collections.Frozen;

namespace EosDashboards.Application.Preferences;

public static class UserPreferencePalettes
{
    public const string ForestGreen = "forestGreen";
    public const string NavyTeal = "navyTeal";
    public const string Turquoise = "turquoise";
    public const string Plum = "plum";
    public const string Amber = "amber";
    public const string Burgundy = "burgundy";

    public static readonly FrozenSet<string> All =
    [
        ForestGreen,
        NavyTeal,
        Turquoise,
        Plum,
        Amber,
        Burgundy,
    ];
}
