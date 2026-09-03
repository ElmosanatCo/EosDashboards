namespace EosDashboards.Domain.Authorization;

public static class SystemRoleCodes
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string DepartmentManager = "DepartmentManager";
    public const string HumanResourcesManager = "HumanResourcesManager";
    public const string ChiefExecutiveOfficer = "ChiefExecutiveOfficer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SystemAdministrator,
        DepartmentManager,
        HumanResourcesManager,
        ChiefExecutiveOfficer,
    };
}
