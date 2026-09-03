using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Domain.Authorization;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Provisioning;

public sealed record ProvisionSystemAdministratorCommand(
    string OrganizationalId,
    string AccountName,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Mobile,
    string? GoogleEmail = null)
{
    public override string ToString() => nameof(ProvisionSystemAdministratorCommand);
}

public sealed record ProvisionSystemAdministratorResult(long UserId, string MaskedMobile)
{
    public override string ToString() =>
        $"System administrator provisioned for user {UserId} with mobile {MaskedMobile}.";
}

public sealed class ProvisionSystemAdministrator(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserRepository users,
    IRoleRepository roles,
    IDepartmentRepository departments,
    IMobileProtector mobileProtector,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IExternalIdentityLinkRepository externalIdentityLinks,
    IUnitOfWork unitOfWork)
{
    private const string SystemAdministratorRoleCode = SystemRoleCodes.SystemAdministrator;
    private const string SystemAdministratorDisplayName = "مدیر سامانه";
    private const string DepartmentManagerRoleCode = SystemRoleCodes.DepartmentManager;
    private const string DepartmentManagerDisplayName = "مدیر بخش";
    private const string SoftwareDepartmentName = "نرم افزار";
    private const string ProvisioningEventCode = "SystemAdministratorProvisioned";
    private const string ProvisioningOperationKey = "ProvisionSystemAdministrator";

    public async Task<ProvisionSystemAdministratorResult> HandleAsync(
        ProvisionSystemAdministratorCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var normalizedOrganizationalId = NormalizeIdentifier(
            command.OrganizationalId,
            nameof(command.OrganizationalId));
        var normalizedAccountName = NormalizeIdentifier(
            command.AccountName,
            nameof(command.AccountName));
        var normalizedUsername = NormalizeIdentifier(command.Username, nameof(command.Username));
        if (!PasswordPolicy.IsValid(command.Password))
        {
            throw new ArgumentException("The password length is not valid.", nameof(command.Password));
        }

        var firstName = NormalizeName(command.FirstName, nameof(command.FirstName));
        var lastName = NormalizeName(command.LastName, nameof(command.LastName));
        var normalizedMobile = NormalizeMobile(command.Mobile);
        var normalizedGoogleEmail = NormalizeOptionalGoogleEmail(command.GoogleEmail);
        var protectedMobile = mobileProtector.Protect(normalizedMobile);
        var maskedMobile = mobileProtector.Mask(normalizedMobile);
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        User? provisionedUser = null;

        await unitOfWork.ExecuteSerializedTransactionAsync(
            ProvisioningOperationKey,
            async transactionCancellationToken =>
            {
                var systemAdministratorRole = await roles.FindByCodeAsync(
                    SystemAdministratorRoleCode,
                    transactionCancellationToken);
                ValidateRole(systemAdministratorRole, SystemAdministratorRoleCode, SystemAdministratorDisplayName);
                var departmentManagerRole = await roles.FindByCodeAsync(
                    DepartmentManagerRoleCode,
                    transactionCancellationToken);
                ValidateRole(departmentManagerRole, DepartmentManagerRoleCode, DepartmentManagerDisplayName);
                var softwareDepartment = await departments.FindByNameAsync(
                    SoftwareDepartmentName,
                    transactionCancellationToken)
                    ?? throw new InvalidOperationException("The software department is not provisioned.");

                var user = await users.FindByOrganizationalIdAsync(
                    normalizedOrganizationalId,
                    transactionCancellationToken);
                var createdSystemAdministratorRole = systemAdministratorRole is null;
                var createdDepartmentManagerRole = departmentManagerRole is null;
                var createdUser = user is null;

                if (createdSystemAdministratorRole)
                {
                    systemAdministratorRole = Role.Create(
                        SystemAdministratorRoleCode,
                        SystemAdministratorDisplayName,
                        true,
                        now);
                    roles.Add(systemAdministratorRole);
                }

                if (createdDepartmentManagerRole)
                {
                    departmentManagerRole = Role.Create(
                        DepartmentManagerRoleCode,
                        DepartmentManagerDisplayName,
                        true,
                        now);
                    roles.Add(departmentManagerRole);
                }

                if (createdUser)
                {
                    user = User.Create(
                        normalizedOrganizationalId,
                        normalizedAccountName,
                        firstName,
                        lastName,
                        protectedMobile,
                        maskedMobile,
                        softwareDepartment.Id,
                        now);
                    users.Add(user);
                }

                if (createdSystemAdministratorRole || createdDepartmentManagerRole || createdUser)
                {
                    await unitOfWork.SaveChangesAsync(transactionCancellationToken);
                }

                if (!createdUser)
                {
                    user!.UpdateProfile(
                        normalizedAccountName,
                        firstName,
                        lastName,
                        protectedMobile,
                        maskedMobile,
                        now);
                    user.Activate(now);
                    user.AssignDepartment(softwareDepartment.Id, now);
                }

                if (normalizedGoogleEmail is not null)
                {
                    var existingGoogleLink = await externalIdentityLinks.FindByUserIdAndProviderAsync(
                        user!.Id,
                        ExternalIdentityProvider.Google,
                        transactionCancellationToken);
                    if (existingGoogleLink is null)
                    {
                        externalIdentityLinks.Add(ExternalIdentityLink.CreatePending(
                            user.Id,
                            ExternalIdentityProvider.Google,
                            normalizedGoogleEmail,
                            now));
                    }
                    else
                    {
                        existingGoogleLink.UpdateApprovedEmail(normalizedGoogleEmail);
                    }
                }

                user!.SetLocalCredentials(
                    normalizedUsername,
                    passwordHasher.Hash(command.Password),
                    now);

                user!.AssignRole(systemAdministratorRole!.Id);
                user.AssignRole(departmentManagerRole!.Id);
                await auditWriter.WriteAsync(
                    new AuditRecord(
                        null,
                        user.Id,
                        ProvisioningEventCode,
                        true,
                        traceId,
                        null),
                    transactionCancellationToken);
                await unitOfWork.SaveChangesAsync(transactionCancellationToken);
                provisionedUser = user;
            },
            cancellationToken);

        return new ProvisionSystemAdministratorResult(provisionedUser!.Id, maskedMobile);
    }

    private static string NormalizeIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required identifier is missing.", parameterName);
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required name is missing.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeMobile(string value)
    {
        var normalized = value?.Trim();
        if (normalized is null ||
            normalized.Length != 11 ||
            normalized[0] != '0' ||
            normalized[1] != '9' ||
            normalized.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException(
                "A normalized Iranian mobile number is required.",
                nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptionalGoogleEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ExternalIdentityLink.NormalizeEmail(value);

    private static void ValidateRole(Role? role, string code, string displayName)
    {
        if (role is not null &&
            (!string.Equals(role.Code, code, StringComparison.Ordinal) ||
             !role.IsActive ||
             !role.IsSystem ||
             !string.Equals(role.DisplayName, displayName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("A fixed system role is not valid.");
        }
    }
}
