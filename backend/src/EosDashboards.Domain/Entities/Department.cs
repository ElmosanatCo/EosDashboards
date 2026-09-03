namespace EosDashboards.Domain.Entities;

public sealed class Department
{
    private Department()
    {
        Name = null!;
    }

    private Department(string name, Department? parentDepartment, DateTimeOffset createdAtUtc)
    {
        Name = name;
        ParentDepartment = parentDepartment;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }

    public string Name { get; private set; }

    public long? ParentDepartmentId { get; private set; }

    public Department? ParentDepartment { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Department CreateRoot(string name, DateTimeOffset createdAtUtc) =>
        new(ValidateName(name), null, createdAtUtc.ToUniversalTime());

    public static Department CreateChild(Department parentDepartment, string name, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(parentDepartment);
        if (parentDepartment.ParentDepartmentId is not null || parentDepartment.ParentDepartment is not null)
        {
            throw new InvalidOperationException("A child department cannot have children.");
        }

        return new Department(ValidateName(name), parentDepartment, createdAtUtc.ToUniversalTime());
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A department name is required.", nameof(name));
        }

        return name.Trim();
    }
}
