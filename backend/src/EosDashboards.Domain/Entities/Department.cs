namespace EosDashboards.Domain.Entities;

public sealed class Department
{
    private Department()
    {
        Name = null!;
    }

    private Department(string name, Department? parentDepartment, DateTime createdAt)
    {
        Name = name;
        ParentDepartment = parentDepartment;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public long Id { get; private set; }

    public string Name { get; private set; }

    public long? ParentDepartmentId { get; private set; }

    public Department? ParentDepartment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Department CreateRoot(string name, DateTime createdAt) =>
        new(ValidateName(name), null, createdAt);

    public static Department CreateChild(Department parentDepartment, string name, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(parentDepartment);
        if (parentDepartment.ParentDepartmentId is not null || parentDepartment.ParentDepartment is not null)
        {
            throw new InvalidOperationException("A child department cannot have children.");
        }

        return new Department(ValidateName(name), parentDepartment, createdAt);
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
