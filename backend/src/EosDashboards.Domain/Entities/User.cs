namespace EosDashboards.Domain.Entities;

public sealed class User
{
    private readonly List<UserRole> _userRoles = [];

    private User(
        string organizationalId,
        string accountName,
        string firstName,
        string lastName,
        string protectedMobileNumber,
        string maskedMobileNumber,
        DateTimeOffset createdAtUtc)
    {
        OrganizationalId = organizationalId;
        AccountName = accountName;
        FirstName = firstName;
        LastName = lastName;
        ProtectedMobileNumber = protectedMobileNumber;
        MaskedMobileNumber = maskedMobileNumber;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public long Id { get; private set; }

    public string OrganizationalId { get; private set; }

    public string AccountName { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string ProtectedMobileNumber { get; private set; }

    public string MaskedMobileNumber { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static User Create(
        string organizationalId,
        string accountName,
        string firstName,
        string lastName,
        string protectedMobileNumber,
        string maskedMobileNumber,
        DateTimeOffset createdAtUtc)
    {
        ValidateRequired(organizationalId, nameof(organizationalId));
        ValidateRequired(accountName, nameof(accountName));
        ValidateRequired(firstName, nameof(firstName));
        ValidateRequired(lastName, nameof(lastName));
        ValidateRequired(protectedMobileNumber, nameof(protectedMobileNumber));
        ValidateRequired(maskedMobileNumber, nameof(maskedMobileNumber));

        return new User(
            organizationalId,
            accountName,
            firstName,
            lastName,
            protectedMobileNumber,
            maskedMobileNumber,
            createdAtUtc.ToUniversalTime());
    }

    public void AssignRole(long roleId)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId));
        }

        if (_userRoles.Any(userRole => userRole.RoleId == roleId))
        {
            return;
        }

        _userRoles.Add(new UserRole(Id, roleId));
    }

    public void UpdateProfile(
        string accountName,
        string firstName,
        string lastName,
        string protectedMobileNumber,
        string maskedMobileNumber,
        DateTimeOffset updatedAtUtc)
    {
        ValidateRequired(accountName, nameof(accountName));
        ValidateRequired(firstName, nameof(firstName));
        ValidateRequired(lastName, nameof(lastName));
        ValidateRequired(protectedMobileNumber, nameof(protectedMobileNumber));
        ValidateRequired(maskedMobileNumber, nameof(maskedMobileNumber));

        AccountName = accountName;
        FirstName = firstName;
        LastName = lastName;
        ProtectedMobileNumber = protectedMobileNumber;
        MaskedMobileNumber = maskedMobileNumber;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset deactivatedAtUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAtUtc = deactivatedAtUtc.ToUniversalTime();
        UpdatedAtUtc = DeactivatedAtUtc.Value;
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }
    }
}
