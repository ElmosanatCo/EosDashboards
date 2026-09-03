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
        DateTime createdAt)
    {
        OrganizationalId = organizationalId;
        AccountName = accountName;
        FirstName = firstName;
        LastName = lastName;
        ProtectedMobileNumber = protectedMobileNumber;
        MaskedMobileNumber = maskedMobileNumber;
        DepartmentId = departmentId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
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

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? DeactivatedAt { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static User Create(
        string organizationalId,
        string accountName,
        string firstName,
        string lastName,
        string protectedMobileNumber,
        string maskedMobileNumber,
        long departmentId,
        DateTime createdAt)
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
            createdAt);
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

    public void AssignDepartment(long departmentId, DateTime updatedAt)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        DepartmentId = departmentId;
        UpdatedAt = updatedAt;
    }

    public void UpdateProfile(
        string accountName,
        string firstName,
        string lastName,
        string protectedMobileNumber,
        string maskedMobileNumber,
        DateTime updatedAt)
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
        UpdatedAt = updatedAt;
    }

    public void SetLocalCredentials(
        string username,
        string passwordHash,
        DateTime updatedAt)
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
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime deactivatedAt)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAt = deactivatedAt;
        UpdatedAt = DeactivatedAt.Value;
    }

    public void Activate(DateTime activatedAt)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        DeactivatedAt = null;
        UpdatedAt = activatedAt;
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }
    }
}
