using System.Collections.Frozen;

namespace EosDashboards.Application.Preferences;

public static class UserPreferencePalettes
{
    public const string Teal = "teal";
    public const string Indigo = "indigo";
    public const string Emerald = "emerald";
    public const string Amber = "amber";
    public const string Orange = "orange";
    public const string Rose = "rose";

    public static readonly FrozenSet<string> All =
    [
        Teal,
        Indigo,
        Emerald,
        Amber,
        Orange,
        Rose,
    ];
}
