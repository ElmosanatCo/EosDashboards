namespace EosDashboards.Domain.Entities;

public sealed class SkillCatalogItem
{
    private SkillCatalogItem()
    {
        Name = null!;
    }

    private SkillCatalogItem(long? departmentId, long? ownerDepartmentId, string name, DateTime createdAt)
    {
        DepartmentId = departmentId;
        OwnerDepartmentId = ownerDepartmentId;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        IsActive = true;
    }

    public long Id { get; private set; }

    public long? DepartmentId { get; private set; }

    public long? OwnerDepartmentId { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static SkillCatalogItem Create(long? departmentId, string name, DateTime createdAt)
    {
        if (departmentId is null or <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A skill name is required.", nameof(name));
        }

        return new SkillCatalogItem(departmentId, null, name.Trim(), createdAt);
    }

    public static SkillCatalogItem CreatePublic(long ownerDepartmentId, string name, DateTime createdAt)
    {
        if (ownerDepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerDepartmentId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A skill name is required.", nameof(name));
        }

        return new SkillCatalogItem(null, ownerDepartmentId, name.Trim(), createdAt);
    }

    public void Rename(string name, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A skill name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }
}
