namespace EosDashboards.Application.Administration;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount);

public sealed record AuditLogListItem(
    long Id,
    DateTime OccurredAt,
    string EventCode,
    bool Succeeded,
    long? ActorUserId,
    string? ActorDisplayName,
    long? SubjectUserId,
    string? SubjectDisplayName,
    string? ClientIpAddress,
    string? ClientDeviceKind);

public sealed record AuditLogQuery(
    DateTime From,
    DateTime To,
    string? EventCode,
    long? ActorUserId,
    long? SubjectUserId,
    bool? Succeeded,
    int PageNumber,
    int PageSize);

public interface IAuditLogReader
{
    Task<PagedResult<AuditLogListItem>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken);
}

public sealed record AdministrationUserListItem(
    long Id, string PersonnelCode, string FirstName, string LastName,
    string? Username, string MaskedMobile, long DepartmentId, string DepartmentName,
    bool IsActive, bool MustChangePassword, IReadOnlyList<long> RoleIds, byte[] RowVersion);

public sealed record AdministrationRoleListItem(long Id, string Code, string DisplayName);

public sealed record DepartmentListItem(long Id, string Name, long? ParentDepartmentId, byte[] RowVersion);

public interface IAdministrationLookupReader
{
    Task<PagedResult<AdministrationUserListItem>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<AdministrationUserListItem?> GetUserAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdministrationRoleListItem>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentListItem>> GetDepartmentsAsync(CancellationToken cancellationToken);
}
