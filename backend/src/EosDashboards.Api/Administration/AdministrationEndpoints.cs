using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Auth;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Security;
using EosDashboards.Application.Administration;
using EosDashboards.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EosDashboards.Api.Administration;

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/administration").WithTags("System administration")
            .RequireAuthorization("SystemAdministrator").AddEndpointFilter<NoStoreEndpointFilter>();
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/audit-logs", GetAuditHistoryAsync);
        group.MapGet("/users", GetUsersAsync);
        group.MapGet("/users/{userId:long}", GetUserAsync);
        group.MapGet("/roles", GetRolesAsync);
        group.MapGet("/departments", GetDepartmentsAsync);
        group.MapPost("/users", CreateUserAsync);
        group.MapPut("/users/{userId:long}", UpdateUserAsync);
        group.MapPut("/users/{userId:long}/active", SetUserActiveAsync);
        group.MapPost("/users/{userId:long}/password-reset", ResetUserPasswordAsync);
        group.MapPost("/departments", CreateDepartmentAsync);
        group.MapPut("/departments/{departmentId:long}", UpdateDepartmentAsync);
        group.MapDelete("/departments/{departmentId:long}", DeleteDepartmentAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDashboardAsync(
        GetSystemAdministrationDashboard dashboard,
        CancellationToken token) => Results.Ok(await dashboard.HandleAsync(token));

    private static async Task<IResult> GetAuditHistoryAsync(HttpContext context, GetAuditHistory history, AuditHistoryRange range = AuditHistoryRange.LastSevenDays, DateTime? from = null, DateTime? to = null, string? eventCode = null, long? actorUserId = null, long? subjectUserId = null, bool? succeeded = null, int pageNumber = 1, int pageSize = 50, CancellationToken token = default)
    {
        var result = await history.HandleAsync(new AuditHistoryQuery(range, from, to, eventCode, actorUserId, subjectUserId, succeeded, pageNumber, pageSize), token);
        return result.IsValid ? Results.Ok(result.Value) : ApiResults.Problem(context, 400, "invalid_audit_query", "The audit filter is invalid.");
    }

    private static async Task<IResult> GetUsersAsync(HttpContext context, IAdministrationLookupReader reader, int pageNumber = 1, int pageSize = 50, CancellationToken token = default)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100) return Invalid(context);
        var users = await reader.GetUsersAsync(pageNumber, pageSize, token);
        return Results.Ok(new PagedResult<ManagedUserResponse>(users.Items.Select(User).ToArray(), users.PageNumber, users.PageSize, users.TotalCount));
    }
    private static async Task<IResult> GetUserAsync(long userId, IAdministrationLookupReader reader, CancellationToken token)
    {
        var user = await reader.GetUserAsync(userId, token);
        return user is null ? Results.NotFound() : Results.Ok(User(user));
    }
    private static async Task<IResult> GetRolesAsync(IAdministrationLookupReader reader, CancellationToken token) => Results.Ok(await reader.GetRolesAsync(token));
    private static async Task<IResult> GetDepartmentsAsync(IAdministrationLookupReader reader, CancellationToken token) => Results.Ok((await reader.GetDepartmentsAsync(token)).Select(Department));

    private static async Task<IResult> CreateUserAsync(HttpContext context, CreateUserRequest request, ManageUsers users, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await users.CreateAsync(actor, new CreateUserCommand(request.PersonnelCode, request.AccountName, request.FirstName, request.LastName, request.Mobile, request.Username, request.TemporaryPassword, request.DepartmentId, request.RoleIds), token);
        return UserResult(context, result, created: true);
    }

    private static async Task<IResult> UpdateUserAsync(long userId, HttpContext context, UpdateUserRequest request, ManageUsers users, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!TryRowVersion(request.RowVersion, out var rowVersion)) return Invalid(context);
        var result = await users.UpdateAsync(actor, new UpdateUserCommand(userId, request.PersonnelCode, request.AccountName, request.FirstName, request.LastName, request.ReplacementMobile, request.Username, request.DepartmentId, request.RoleIds, rowVersion), token);
        return UserResult(context, result, created: false);
    }

    private static async Task<IResult> SetUserActiveAsync(long userId, HttpContext context, SetUserActiveRequest request, ManageUsers users, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!TryRowVersion(request.RowVersion, out var rowVersion)) return Invalid(context);
        return UserResult(context, await users.SetActiveAsync(actor, new SetUserActiveCommand(userId, request.IsActive, rowVersion), token), false);
    }

    private static async Task<IResult> ResetUserPasswordAsync(long userId, HttpContext context, ResetUserPasswordRequest request, ManageUsers users, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!TryRowVersion(request.RowVersion, out var rowVersion)) return Invalid(context);
        return UserResult(context, await users.ResetPasswordAsync(actor, new ResetUserPasswordCommand(userId, request.TemporaryPassword, rowVersion), token), false);
    }

    private static async Task<IResult> CreateDepartmentAsync(HttpContext context, CreateDepartmentRequest request, ManageDepartments departments, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return DepartmentResult(context, await departments.CreateAsync(actor, new CreateDepartmentCommand(request.Name, request.ParentDepartmentId), token), true);
    }

    private static async Task<IResult> UpdateDepartmentAsync(long departmentId, HttpContext context, UpdateDepartmentRequest request, ManageDepartments departments, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!TryRowVersion(request.RowVersion, out var rowVersion)) return Invalid(context);
        return DepartmentResult(context, await departments.UpdateAsync(actor, new UpdateDepartmentCommand(departmentId, request.Name, request.ParentDepartmentId, rowVersion), token), false);
    }

    private static async Task<IResult> DeleteDepartmentAsync(long departmentId, HttpContext context, [FromBody] DeleteDepartmentRequest request, ManageDepartments departments, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!TryRowVersion(request.RowVersion, out var rowVersion)) return Invalid(context);
        return DepartmentResult(context, await departments.DeleteAsync(actor, new DeleteDepartmentCommand(departmentId, rowVersion), token), false);
    }

    private static IResult UserResult(HttpContext context, ManageUserResult result, bool created) => result.Status switch
    {
        ManageUserStatus.Succeeded => created ? Results.Created($"/api/v1/administration/users/{result.User!.Id}", User(result.User!)) : Results.Ok(User(result.User!)),
        ManageUserStatus.NotFound => ApiResults.Problem(context, 404, "user_not_found", "The requested user was not found."),
        ManageUserStatus.DuplicateOrganizationalId => ApiResults.Problem(context, 409, "personnel_code_conflict", "The personnel code is already in use."),
        ManageUserStatus.DuplicateUsername => ApiResults.Problem(context, 409, "username_conflict", "The username is already in use."),
        ManageUserStatus.Conflict => ApiResults.Problem(context, 409, "concurrency_conflict", "The record has changed. Refresh and try again."),
        ManageUserStatus.LastSystemAdministrator => ApiResults.Problem(context, 409, "last_system_administrator", "At least one active System Administrator is required."),
        _ => Invalid(context),
    };

    private static IResult DepartmentResult(HttpContext context, DepartmentOperationResult result, bool created) => result.Status switch
    {
        DepartmentOperationStatus.Succeeded => created ? Results.Created($"/api/v1/administration/departments/{result.Department!.Id}", Department(result.Department!)) : Results.Ok(Department(result.Department!)),
        DepartmentOperationStatus.NotFound => ApiResults.Problem(context, 404, "department_not_found", "The requested department was not found."),
        DepartmentOperationStatus.DuplicateName => ApiResults.Problem(context, 409, "department_name_conflict", "The department name is already in use."),
        DepartmentOperationStatus.NotEmpty => ApiResults.Problem(context, 409, "department_not_empty", "Move users and child departments first."),
        DepartmentOperationStatus.Conflict => ApiResults.Problem(context, 409, "concurrency_conflict", "The record has changed. Refresh and try again."),
        _ => ApiResults.Problem(context, 400, "department_hierarchy_invalid", "The department hierarchy is invalid."),
    };

    private static ManagedUserResponse User(User user) => new(user.Id, user.OrganizationalId, user.AccountName, user.FirstName, user.LastName, user.Username, user.MaskedMobileNumber, user.DepartmentId, null, user.IsActive, user.MustChangePassword, user.UserRoles.Select(item => item.RoleId).ToArray(), Convert.ToBase64String(user.RowVersion));
    private static ManagedUserResponse User(AdministrationUserListItem user) => new(user.Id, user.PersonnelCode, user.AccountName, user.FirstName, user.LastName, user.Username, user.MaskedMobile, user.DepartmentId, user.DepartmentName, user.IsActive, user.MustChangePassword, user.RoleIds.ToArray(), Convert.ToBase64String(user.RowVersion));
    private static ManagedDepartmentResponse Department(Department department) => new(department.Id, department.Name, department.ParentDepartmentId, Convert.ToBase64String(department.RowVersion));
    private static ManagedDepartmentResponse Department(DepartmentListItem department) => new(department.Id, department.Name, department.ParentDepartmentId, Convert.ToBase64String(department.RowVersion));
    private static bool TryActor(HttpContext context, out long actor) => SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out actor);
    private static bool TryRowVersion(string value, out byte[] rowVersion) { try { rowVersion = Convert.FromBase64String(value); return rowVersion.Length > 0; } catch (FormatException) { rowVersion = []; return false; } }
    private static IResult Unauthorized(HttpContext context) => ApiResults.Problem(context, 401, "invalid_access_token", "Authentication is required.");
    private static IResult Invalid(HttpContext context) => ApiResults.Problem(context, 400, "invalid_administration_request", "The request is invalid.");
}
