using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Domain.Authorization;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Administration;

public enum ManageUserStatus
{
    Succeeded,
    NotFound,
    Invalid,
    DuplicateOrganizationalId,
    DuplicateUsername,
    Conflict,
    LastSystemAdministrator,
}

public sealed record ManageUserResult(ManageUserStatus Status, User? User)
{
    public override string ToString() => nameof(ManageUserResult);
}

public sealed record CreateUserCommand(
    string OrganizationalId,
    string FirstName,
    string LastName,
    string Mobile,
    string? Username,
    string TemporaryPassword,
    long DepartmentId,
    IReadOnlyCollection<long> RoleIds)
{
    public override string ToString() => nameof(CreateUserCommand);
}

public sealed record UpdateUserCommand(
    long UserId,
    string OrganizationalId,
    string FirstName,
    string LastName,
    string? ReplacementMobile,
    string Username,
    long DepartmentId,
    IReadOnlyCollection<long> RoleIds,
    byte[] ExpectedRowVersion)
{
    public override string ToString() => nameof(UpdateUserCommand);
}

public sealed record SetUserActiveCommand(long UserId, bool IsActive, byte[] ExpectedRowVersion)
{
    public override string ToString() => nameof(SetUserActiveCommand);
}

public sealed record ResetUserPasswordCommand(long UserId, string TemporaryPassword, byte[] ExpectedRowVersion)
{
    public override string ToString() => nameof(ResetUserPasswordCommand);
}

public sealed class ManageUsers(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IRoleRepository roles,
    IDepartmentRepository departments,
    IUserSessionRepository sessions,
    IMobileProtector mobileProtector,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private const string OperationKey = "ManageUsers";

    public async Task<ManageUserResult> CreateAsync(
        long actorUserId,
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || actorUserId <= 0)
        {
            return Invalid();
        }

        if (!PasswordPolicy.IsValid(command.TemporaryPassword))
        {
            return Invalid();
        }

        var organizationalId = NormalizeRequired(command.OrganizationalId);
        var username = NormalizeRequired(command.Username ?? organizationalId);
        var mobile = NormalizeMobile(command.Mobile);
        if (organizationalId is null || username is null || mobile is null ||
            !TryNormalizeProfile(command.FirstName, command.LastName, out var profile) ||
            command.DepartmentId <= 0)
        {
            return Invalid();
        }

        ManageUserResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(
            OperationKey,
            async token =>
            {
                if (await users.FindByOrganizationalIdAsync(organizationalId, token) is not null)
                {
                    result = new ManageUserResult(ManageUserStatus.DuplicateOrganizationalId, null);
                    return;
                }

                if (await users.FindByUsernameAsync(username, token) is not null)
                {
                    result = new ManageUserResult(ManageUserStatus.DuplicateUsername, null);
                    return;
                }

                if (await departments.GetByIdAsync(command.DepartmentId, token) is null ||
                    !await AreValidFixedRolesAsync(command.RoleIds, true, token))
                {
                    result = Invalid();
                    return;
                }

                var now = clock.Now;
                var user = User.Create(
                    organizationalId,
                    profile.FirstName,
                    profile.LastName,
                    mobileProtector.Protect(mobile),
                    mobileProtector.Mask(mobile),
                    command.DepartmentId,
                    now);
                users.Add(user);
                await unitOfWork.SaveChangesAsync(token);
                user.SetTemporaryLocalCredentials(username, passwordHasher.Hash(command.TemporaryPassword), now);
                user.ReplaceRoles(command.RoleIds.Distinct().ToArray(), now);
                await WriteAuditAsync(actorUserId, user.Id, AdministrationAuditEvents.UserCreated, token);
                await unitOfWork.SaveChangesAsync(token);
                result = new ManageUserResult(ManageUserStatus.Succeeded, user);
            },
            cancellationToken);
        return result;
    }

    public async Task<ManageUserResult> UpdateAsync(
        long actorUserId,
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || actorUserId <= 0 || command.UserId <= 0 ||
            !HasExpectedRowVersion(command.ExpectedRowVersion))
        {
            return Invalid();
        }

        var organizationalId = NormalizeRequired(command.OrganizationalId);
        var username = NormalizeRequired(command.Username);
        var replacementMobile = command.ReplacementMobile is null ? null : NormalizeMobile(command.ReplacementMobile);
        if (organizationalId is null || username is null ||
            (command.ReplacementMobile is not null && replacementMobile is null) ||
            !TryNormalizeProfile(command.FirstName, command.LastName, out var profile) ||
            command.DepartmentId <= 0)
        {
            return Invalid();
        }

        ManageUserResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(
            OperationKey,
            async token =>
            {
                var user = await users.GetForUpdateAsync(command.UserId, token);
                if (user is null)
                {
                    result = new ManageUserResult(ManageUserStatus.NotFound, null);
                    return;
                }

                if (!user.RowVersion.SequenceEqual(command.ExpectedRowVersion))
                {
                    result = new ManageUserResult(ManageUserStatus.Conflict, null);
                    return;
                }

                var sameOrganizationalId = string.Equals(user.OrganizationalId, organizationalId, StringComparison.Ordinal);
                if (!sameOrganizationalId && await users.FindByOrganizationalIdAsync(organizationalId, token) is not null)
                {
                    result = new ManageUserResult(ManageUserStatus.DuplicateOrganizationalId, null);
                    return;
                }

                var sameUsername = string.Equals(user.Username, username, StringComparison.Ordinal);
                if (!sameUsername && await users.FindByUsernameAsync(username, token) is not null)
                {
                    result = new ManageUserResult(ManageUserStatus.DuplicateUsername, null);
                    return;
                }

                if (await departments.GetByIdAsync(command.DepartmentId, token) is null ||
                    !await AreValidFixedRolesAsync(command.RoleIds, user.IsActive, token))
                {
                    result = Invalid();
                    return;
                }

                if (await IsRemovingLastSystemAdministratorRoleAsync(user, command.RoleIds, token))
                {
                    result = new ManageUserResult(ManageUserStatus.LastSystemAdministrator, null);
                    return;
                }

                var now = clock.Now;
                var roleChanged = !HaveSameRoles(user.UserRoles.Select(role => role.RoleId), command.RoleIds);
                var departmentChanged = user.DepartmentId != command.DepartmentId;
                if (replacementMobile is null)
                {
                    user.UpdateProfile(profile.FirstName, profile.LastName,
                        user.ProtectedMobileNumber, user.MaskedMobileNumber, now);
                }
                else
                {
                    user.UpdateProfile(profile.FirstName, profile.LastName,
                        mobileProtector.Protect(replacementMobile), mobileProtector.Mask(replacementMobile), now);
                }

                user.UpdateOrganizationalId(organizationalId, now);
                user.UpdateUsername(username, now);
                user.AssignDepartment(command.DepartmentId, now);
                user.ReplaceRoles(command.RoleIds.Distinct().ToArray(), now);
                await RevokeTargetSessionsAsync(actorUserId, user.Id, now);
                await WriteAuditAsync(actorUserId, user.Id, AdministrationAuditEvents.UserUpdated, token);
                if (roleChanged)
                {
                    await WriteAuditAsync(actorUserId, user.Id, AdministrationAuditEvents.UserRolesChanged, token);
                }

                if (departmentChanged)
                {
                    await WriteAuditAsync(actorUserId, user.Id, AdministrationAuditEvents.UserDepartmentChanged, token);
                }

                await unitOfWork.SaveChangesAsync(token);
                result = new ManageUserResult(ManageUserStatus.Succeeded, user);
            },
            cancellationToken);
        return result;
    }

    public async Task<ManageUserResult> SetActiveAsync(
        long actorUserId,
        SetUserActiveCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || actorUserId <= 0 || command.UserId <= 0 ||
            !HasExpectedRowVersion(command.ExpectedRowVersion))
        {
            return Invalid();
        }

        ManageUserResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(
            OperationKey,
            async token =>
            {
                var user = await users.GetForUpdateAsync(command.UserId, token);
                if (user is null)
                {
                    result = new ManageUserResult(ManageUserStatus.NotFound, null);
                    return;
                }

                if (!user.RowVersion.SequenceEqual(command.ExpectedRowVersion))
                {
                    result = new ManageUserResult(ManageUserStatus.Conflict, null);
                    return;
                }

                if (!command.IsActive && user.IsActive && await IsLastActiveSystemAdministratorAsync(user, token))
                {
                    result = new ManageUserResult(ManageUserStatus.LastSystemAdministrator, null);
                    return;
                }

                var now = clock.Now;
                if (command.IsActive)
                {
                    try
                    {
                        user.Activate(now);
                    }
                    catch (InvalidOperationException)
                    {
                        result = Invalid();
                        return;
                    }
                }
                else
                {
                    user.Deactivate(now);
                    await RevokeTargetSessionsAsync(actorUserId, user.Id, now);
                }

                await WriteAuditAsync(actorUserId, user.Id,
                    command.IsActive ? AdministrationAuditEvents.UserActivated : AdministrationAuditEvents.UserDeactivated,
                    token);
                await unitOfWork.SaveChangesAsync(token);
                result = new ManageUserResult(ManageUserStatus.Succeeded, user);
            },
            cancellationToken);
        return result;
    }

    public async Task<ManageUserResult> ResetPasswordAsync(
        long actorUserId,
        ResetUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || actorUserId <= 0 || command.UserId <= 0 ||
            !HasExpectedRowVersion(command.ExpectedRowVersion))
        {
            return Invalid();
        }

        if (!PasswordPolicy.IsValid(command.TemporaryPassword))
        {
            return Invalid();
        }

        ManageUserResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(
            OperationKey,
            async token =>
            {
                var user = await users.GetForUpdateAsync(command.UserId, token);
                if (user is null)
                {
                    result = new ManageUserResult(ManageUserStatus.NotFound, null);
                    return;
                }

                if (!user.RowVersion.SequenceEqual(command.ExpectedRowVersion))
                {
                    result = new ManageUserResult(ManageUserStatus.Conflict, null);
                    return;
                }

                if (!user.IsActive || user.Username is null)
                {
                    result = Invalid();
                    return;
                }

                var now = clock.Now;
                user.SetTemporaryLocalCredentials(user.Username, passwordHasher.Hash(command.TemporaryPassword), now);
                await RevokeTargetSessionsAsync(actorUserId, user.Id, now);
                await WriteAuditAsync(actorUserId, user.Id, AdministrationAuditEvents.UserPasswordReset, token);
                await unitOfWork.SaveChangesAsync(token);
                result = new ManageUserResult(ManageUserStatus.Succeeded, user);
            },
            cancellationToken);
        return result;
    }

    private async Task<bool> AreValidFixedRolesAsync(
        IReadOnlyCollection<long>? roleIds,
        bool rolesAreRequired,
        CancellationToken cancellationToken)
    {
        if (roleIds is null || roleIds.Any(id => id <= 0) || (rolesAreRequired && roleIds.Count == 0))
        {
            return false;
        }

        var distinctRoleIds = roleIds.Distinct().ToArray();
        if (distinctRoleIds.Length == 0)
        {
            return true;
        }
        var selectedRoles = await roles.GetByIdsAsync(distinctRoleIds, cancellationToken);
        return selectedRoles.Count == distinctRoleIds.Length && selectedRoles.All(role =>
            role.IsActive && role.IsSystem && SystemRoleCodes.All.Contains(role.Code));
    }

    private async Task<bool> IsLastActiveSystemAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.IsActive)
        {
            return false;
        }

        var systemAdministratorRole = await roles.FindByCodeAsync(SystemRoleCodes.SystemAdministrator, cancellationToken);
        if (systemAdministratorRole is null || !systemAdministratorRole.IsActive || !systemAdministratorRole.IsSystem ||
            !user.UserRoles.Any(role => role.RoleId == systemAdministratorRole.Id))
        {
            return false;
        }

        return await users.CountActiveWithRoleAsync(systemAdministratorRole.Id, cancellationToken) <= 1;
    }

    private async Task<bool> IsRemovingLastSystemAdministratorRoleAsync(
        User user,
        IReadOnlyCollection<long> replacementRoleIds,
        CancellationToken cancellationToken)
    {
        if (!user.IsActive)
        {
            return false;
        }

        var systemAdministratorRole = await roles.FindByCodeAsync(SystemRoleCodes.SystemAdministrator, cancellationToken);
        return systemAdministratorRole is { IsActive: true, IsSystem: true } &&
               user.UserRoles.Any(role => role.RoleId == systemAdministratorRole.Id) &&
               !replacementRoleIds.Contains(systemAdministratorRole.Id) &&
               await users.CountActiveWithRoleAsync(systemAdministratorRole.Id, cancellationToken) <= 1;
    }

    private async Task RevokeTargetSessionsAsync(long actorUserId, long targetUserId, DateTime now)
    {
        if (actorUserId == targetUserId)
        {
            return;
        }

        foreach (var session in await sessions.GetActiveByUserIdAsync(targetUserId, now, CancellationToken.None))
        {
            session.Revoke(SessionRevocationReason.AdministrativeChange, now);
        }
    }

    private Task WriteAuditAsync(long actorUserId, long subjectUserId, string eventCode, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new AuditRecord(actorUserId, subjectUserId, eventCode, true, correlationContext.TraceId, null), cancellationToken);

    private static bool HaveSameRoles(IEnumerable<long> existingRoleIds, IReadOnlyCollection<long> replacementRoleIds) =>
        existingRoleIds.Order().SequenceEqual(replacementRoleIds.Distinct().Order());

    private static bool HasExpectedRowVersion(byte[]? rowVersion) => rowVersion is { Length: > 0 };

    private static ManageUserResult Invalid() => new(ManageUserStatus.Invalid, null);

    private static string? NormalizeRequired(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool TryNormalizeProfile(string? firstName, string? lastName, out (string FirstName, string LastName) profile)
    {
        profile = default;
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return false;
        }

        profile = (firstName.Trim(), lastName.Trim());
        return true;
    }

    private static string? NormalizeMobile(string? value)
    {
        var normalized = value?.Trim();
        return normalized is { Length: 11 } && normalized[0] == '0' && normalized[1] == '9' &&
               normalized.All(character => character is >= '0' and <= '9')
            ? normalized
            : null;
    }
}
