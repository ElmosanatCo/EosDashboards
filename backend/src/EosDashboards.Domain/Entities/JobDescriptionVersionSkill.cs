namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionVersionSkill
{
    private JobDescriptionVersionSkill() { }

    public JobDescriptionVersionSkill(long jobDescriptionVersionId, long skillCatalogItemId)
    {
        if (jobDescriptionVersionId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jobDescriptionVersionId));
        }

        if (skillCatalogItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillCatalogItemId));
        }

        JobDescriptionVersionId = jobDescriptionVersionId;
        SkillCatalogItemId = skillCatalogItemId;
    }

    public long JobDescriptionVersionId { get; private set; }

    public long SkillCatalogItemId { get; private set; }

    public JobDescriptionVersion? JobDescriptionVersion { get; private set; }

    public SkillCatalogItem? SkillCatalogItem { get; private set; }
}
