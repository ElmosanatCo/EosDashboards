namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionRecord
{
    private readonly List<JobDescriptionVersion> _versions = [];

    private JobDescriptionRecord()
    {
        PersonName = null!;
    }

    private JobDescriptionRecord(long departmentId, string personName, DateTime createdAt)
    {
        DepartmentId = departmentId;
        PersonName = personName;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public long Id { get; private set; }

    public long DepartmentId { get; private set; }

    public string PersonName { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<JobDescriptionVersion> Versions => _versions.AsReadOnly();

    public static JobDescriptionRecord Create(long departmentId, string personName, DateTime createdAt)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(personName))
        {
            throw new ArgumentException("A person name is required.", nameof(personName));
        }

        return new JobDescriptionRecord(departmentId, personName.Trim(), createdAt);
    }

    public void RenamePerson(string personName, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(personName))
        {
            throw new ArgumentException("A person name is required.", nameof(personName));
        }

        PersonName = personName.Trim();
        UpdatedAt = updatedAt;
    }

    public void MoveToDepartment(long departmentId, DateTime updatedAt)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        DepartmentId = departmentId;
        UpdatedAt = updatedAt;
    }
}
