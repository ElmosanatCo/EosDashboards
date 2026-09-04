namespace EosDashboards.Domain.Entities;

public sealed class TaskCatalogItem
{
    private readonly List<TaskCatalogRequiredSkill> _requiredSkills = [];

    private TaskCatalogItem()
    {
        Title = null!;
    }

    private TaskCatalogItem(long departmentId, string title, bool isProject, DateTime createdAt)
    {
        DepartmentId = departmentId;
        Title = title;
        IsProject = isProject;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        IsActive = true;
    }

    public long Id { get; private set; }

    public long DepartmentId { get; private set; }

    public string Title { get; private set; }

    public bool IsProject { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<TaskCatalogRequiredSkill> RequiredSkills => _requiredSkills.AsReadOnly();

    public IReadOnlyCollection<long> RequiredSkillIds => _requiredSkills.Select(item => item.SkillCatalogItemId).ToArray();

    public static TaskCatalogItem Create(long departmentId, string title, bool isProject, DateTime createdAt)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A task title is required.", nameof(title));
        }

        return new TaskCatalogItem(departmentId, title.Trim(), isProject, createdAt);
    }

    public void AddRequiredSkill(long skillId)
    {
        if (skillId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillId));
        }

        if (_requiredSkills.All(item => item.SkillCatalogItemId != skillId))
        {
            _requiredSkills.Add(new TaskCatalogRequiredSkill(Id, skillId));
        }
    }

    public void RemoveRequiredSkill(long skillId)
    {
        _requiredSkills.RemoveAll(item => item.SkillCatalogItemId == skillId);
    }

    public void Rename(string title, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A task title is required.", nameof(title));
        }

        Title = title.Trim();
        UpdatedAt = updatedAt;
    }

    public void SetProject(bool isProject, DateTime updatedAt)
    {
        IsProject = isProject;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTime updatedAt)
    {
        IsActive = true;
        UpdatedAt = updatedAt;
    }
}
