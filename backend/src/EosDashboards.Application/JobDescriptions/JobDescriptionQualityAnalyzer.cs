using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.JobDescriptions;

public sealed record JobDescriptionQualityFinding(
    string Code,
    string Message,
    string ActionTarget,
    long? TaskCatalogItemId = null,
    long? SkillCatalogItemId = null);

public static class JobDescriptionQualityAnalyzer
{
    public static IReadOnlyList<JobDescriptionQualityFinding> Analyze(
        JobDescriptionVersion version,
        IReadOnlyCollection<TaskCatalogItem> taskCatalog,
        IReadOnlyDictionary<long, string>? skillNames = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(taskCatalog);

        var catalogById = taskCatalog.ToDictionary(item => item.Id);
        var requiredSkillIds = new HashSet<long>();
        var findings = new List<JobDescriptionQualityFinding>();

        foreach (var skill in version.UnresolvedSkills.OrderBy(item => item.SortOrder))
        {
            findings.Add(new JobDescriptionQualityFinding(
                "unresolved-skill",
                $"مهارت واردشده «{skill.RawName}» با کاتالوگ تطبیق داده نشده است.",
                "skills"));
        }

        foreach (var task in version.UnresolvedTasks.OrderBy(item => item.SortOrder))
        {
            findings.Add(new JobDescriptionQualityFinding(
                "unresolved-task",
                $"وظیفه واردشده «{task.RawTitle}» با کاتالوگ تطبیق داده نشده است.",
                $"task:{task.SortOrder}/title"));
        }

        foreach (var task in version.Tasks.OrderBy(item => item.SortOrder))
        {
            if (!task.StartDate.HasValue)
            {
                findings.Add(new JobDescriptionQualityFinding(
                    "missing-task-start-date",
                    $"تاریخ شروع وظیفه «{task.Title}» وارد نشده است.",
                    $"task:{task.SortOrder}/startDate",
                    task.TaskCatalogItemId));
            }

            if (!task.WeeklyHours.HasValue)
            {
                findings.Add(new JobDescriptionQualityFinding(
                    "missing-weekly-hours",
                    $"متوسط ساعت کاری هفتگی وظیفه «{task.Title}» وارد نشده است.",
                    $"task:{task.SortOrder}/weeklyHours",
                    task.TaskCatalogItemId));
            }

            if (!catalogById.TryGetValue(task.TaskCatalogItemId, out var catalogTask))
            {
                findings.Add(new JobDescriptionQualityFinding(
                    "uncatalogued-task",
                    $"عنوان وظیفه «{task.Title}» در کاتالوگ این بخش یافت نشد.",
                    $"task:{task.SortOrder}/title",
                    task.TaskCatalogItemId));
                continue;
            }

            requiredSkillIds.UnionWith(catalogTask.RequiredSkillIds);
        }

        var selectedSkillIds = version.SkillIds.ToHashSet();
        foreach (var missingSkillId in requiredSkillIds.Except(selectedSkillIds).Order())
        {
            findings.Add(new JobDescriptionQualityFinding(
                "missing-required-skill",
                $"مهارت موردنیاز «{DisplaySkillName(missingSkillId, skillNames)}» برای وظایف انتخاب‌شده ثبت نشده است.",
                $"task:{FirstTaskUsingSkill(version, catalogById, missingSkillId)}/skills",
                SkillCatalogItemId: missingSkillId));
        }

        return findings;
    }

    private static string DisplaySkillName(long skillId, IReadOnlyDictionary<long, string>? skillNames) =>
        skillNames is not null && skillNames.TryGetValue(skillId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : $"شناسه {skillId}";

    private static int FirstTaskUsingSkill(
        JobDescriptionVersion version,
        IReadOnlyDictionary<long, TaskCatalogItem> catalogById,
        long skillId)
    {
        return version.Tasks
            .Where(task => catalogById.TryGetValue(task.TaskCatalogItemId, out var catalogTask) &&
                           catalogTask.RequiredSkillIds.Contains(skillId))
            .Select(task => task.SortOrder)
            .FirstOrDefault();
    }
}
