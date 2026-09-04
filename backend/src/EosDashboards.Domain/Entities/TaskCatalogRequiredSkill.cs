namespace EosDashboards.Domain.Entities;

public sealed class TaskCatalogRequiredSkill
{
    private TaskCatalogRequiredSkill()
    {
    }

    public TaskCatalogRequiredSkill(long taskCatalogItemId, long skillCatalogItemId)
    {
        if (taskCatalogItemId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taskCatalogItemId));
        }

        if (skillCatalogItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillCatalogItemId));
        }

        TaskCatalogItemId = taskCatalogItemId;
        SkillCatalogItemId = skillCatalogItemId;
    }

    public long TaskCatalogItemId { get; private set; }

    public long SkillCatalogItemId { get; private set; }

    public TaskCatalogItem? TaskCatalogItem { get; private set; }

    public SkillCatalogItem? SkillCatalogItem { get; private set; }
}
