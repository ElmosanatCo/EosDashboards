namespace EosDashboards.Application.Administration;

public static class AdministrationAuditEvents
{
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserRolesChanged = "UserRolesChanged";
    public const string UserDepartmentChanged = "UserDepartmentChanged";
    public const string UserActivated = "UserActivated";
    public const string UserDeactivated = "UserDeactivated";
    public const string UserPasswordReset = "UserPasswordReset";
}
