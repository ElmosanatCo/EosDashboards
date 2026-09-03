namespace EosDashboards.Api.Administration;

public sealed record CreateUserRequest(string PersonnelCode, string FirstName, string LastName, string Mobile, string? Username, string TemporaryPassword, long DepartmentId, long[] RoleIds)
{ public override string ToString() => nameof(CreateUserRequest); }
public sealed record UpdateUserRequest(string PersonnelCode, string FirstName, string LastName, string? ReplacementMobile, string Username, long DepartmentId, long[] RoleIds, string RowVersion)
{ public override string ToString() => nameof(UpdateUserRequest); }
public sealed record SetUserActiveRequest(bool IsActive, string RowVersion)
{ public override string ToString() => nameof(SetUserActiveRequest); }
public sealed record ResetUserPasswordRequest(string TemporaryPassword, string RowVersion)
{ public override string ToString() => nameof(ResetUserPasswordRequest); }
public sealed record CreateDepartmentRequest(string Name, long? ParentDepartmentId)
{ public override string ToString() => nameof(CreateDepartmentRequest); }
public sealed record UpdateDepartmentRequest(string Name, long? ParentDepartmentId, string RowVersion)
{ public override string ToString() => nameof(UpdateDepartmentRequest); }
public sealed record DeleteDepartmentRequest(string RowVersion)
{ public override string ToString() => nameof(DeleteDepartmentRequest); }
public sealed record ManagedUserResponse(long Id, string PersonnelCode, string FirstName, string LastName, string? Username, string MaskedMobile, long DepartmentId, string? DepartmentName, bool IsActive, bool MustChangePassword, long[] RoleIds, string RowVersion);
public sealed record ManagedDepartmentResponse(long Id, string Name, long? ParentDepartmentId, string RowVersion);
public sealed record AuditLogResponse(long Id, DateTime OccurredAt, string EventCode, bool Succeeded, long? ActorUserId, string? ActorDisplayName, long? SubjectUserId, string? SubjectDisplayName, string? ClientIpAddress, string? ClientDeviceKind);
