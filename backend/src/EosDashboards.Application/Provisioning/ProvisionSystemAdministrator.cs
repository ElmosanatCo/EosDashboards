using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Provisioning;

public sealed record ProvisionSystemAdministratorCommand(
    string OrganizationalId,
    string AccountName,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Mobile)
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
    IMobileProtector mobileProtector,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private const string SystemAdministratorRoleCode = "SystemAdministrator";
    private const string SystemAdministratorDisplayName = "مدیر سامانه";
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
        var protectedMobile = mobileProtector.Protect(normalizedMobile);
        var maskedMobile = mobileProtector.Mask(normalizedMobile);
        var now = clock.UtcNow;
        var traceId = correlationContext.TraceId;
        User? provisionedUser = null;

        await unitOfWork.ExecuteSerializedTransactionAsync(
            ProvisioningOperationKey,
            async transactionCancellationToken =>
            {
                var role = await roles.FindByCodeAsync(
                    SystemAdministratorRoleCode,
                    transactionCancellationToken);
                if (role is not null &&
                    (!string.Equals(
                         role.Code,
                         SystemAdministratorRoleCode,
                         StringComparison.Ordinal) ||
                     !role.IsActive ||
                     !role.IsSystem ||
                     !string.Equals(
                         role.DisplayName,
                         SystemAdministratorDisplayName,
                         StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        "The system administrator role is not valid.");
                }

                var user = await users.FindByOrganizationalIdAsync(
                    normalizedOrganizationalId,
                    transactionCancellationToken);
                var createdRole = role is null;
                var createdUser = user is null;

                if (createdRole)
                {
                    role = Role.Create(
                        SystemAdministratorRoleCode,
                        SystemAdministratorDisplayName,
                        true,
                        now);
                    roles.Add(role);
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
                        now);
                    users.Add(user);
                }

                if (createdRole || createdUser)
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
                }

                user!.SetLocalCredentials(
                    normalizedUsername,
                    passwordHasher.Hash(command.Password),
                    now);

                user!.AssignRole(role!.Id);
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
}
