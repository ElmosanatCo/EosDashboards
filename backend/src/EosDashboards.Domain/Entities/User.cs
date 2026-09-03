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
        long departmentId,
        DateTimeOffset createdAtUtc)
    {
        OrganizationalId = organizationalId;
        AccountName = accountName;
        FirstName = firstName;
        LastName = lastName;
        ProtectedMobileNumber = protectedMobileNumber;
        MaskedMobileNumber = maskedMobileNumber;
        DepartmentId = departmentId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public long Id { get; private set; }

    public string OrganizationalId { get; private set; }

    public string AccountName { get; private set; }

    public string? Username { get; private set; }

    public string? PasswordHash { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string ProtectedMobileNumber { get; private set; }

    public string MaskedMobileNumber { get; private set; }

    public long DepartmentId { get; private set; }

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
        long departmentId,
        DateTimeOffset createdAtUtc)
    {
        ValidateRequired(organizationalId, nameof(organizationalId));
        ValidateRequired(accountName, nameof(accountName));
        ValidateRequired(firstName, nameof(firstName));
        ValidateRequired(lastName, nameof(lastName));
        ValidateRequired(protectedMobileNumber, nameof(protectedMobileNumber));
        ValidateRequired(maskedMobileNumber, nameof(maskedMobileNumber));
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        return new User(
            organizationalId,
            accountName,
            firstName,
            lastName,
            protectedMobileNumber,
            maskedMobileNumber,
            departmentId,
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

    public void AssignDepartment(long departmentId, DateTimeOffset updatedAtUtc)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        DepartmentId = departmentId;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
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

    public void SetLocalCredentials(
        string username,
        string passwordHash,
        DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("A username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        Username = username.Trim().ToUpperInvariant();
        PasswordHash = passwordHash;
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

    public void Activate(DateTimeOffset activatedAtUtc)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        DeactivatedAtUtc = null;
        UpdatedAtUtc = activatedAtUtc.ToUniversalTime();
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }
    }
}
