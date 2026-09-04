namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionVersionUnresolvedSkill
{
    private JobDescriptionVersionUnresolvedSkill()
    {
        RawName = null!;
    }

    private JobDescriptionVersionUnresolvedSkill(string rawName, int sortOrder)
    {
        RawName = rawName;
        SortOrder = sortOrder;
    }

    public long Id { get; private set; }

    public long JobDescriptionVersionId { get; private set; }

    public string RawName { get; private set; }

    public int SortOrder { get; private set; }

    public static JobDescriptionVersionUnresolvedSkill Create(string rawName, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            throw new ArgumentException("An unresolved skill name is required.", nameof(rawName));
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        return new JobDescriptionVersionUnresolvedSkill(rawName.Trim(), sortOrder);
    }
}
