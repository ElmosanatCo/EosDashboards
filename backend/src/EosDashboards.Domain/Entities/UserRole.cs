namespace EosDashboards.Domain.Entities;

public sealed class UserRole
{
    internal UserRole(long userId, long roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public long UserId { get; private set; }

    public long RoleId { get; private set; }

}
