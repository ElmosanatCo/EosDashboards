using System.Globalization;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.JobDescriptions;

public sealed class CompareJobDescriptionVersions(
    IJobDescriptionComparisonReader reader,
    IJobDescriptionScope scope)
{
    public async Task<JobDescriptionComparisonResult?> HandleAsync(
        long actorUserId,
        long versionId,
        CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return null;
        }

        var versions = await reader.GetAsync(versionId, cancellationToken);
        if (versions is null)
        {
            return null;
        }

        var current = Snapshot(versions.Current);
        var previous = versions.Previous is null ? null : Snapshot(versions.Previous);
        return new(
            versions.Current.Id,
            versions.Previous?.Id,
            current,
            previous,
            previous is null ? [] : Compare(previous, current));
    }

    private static IReadOnlyList<JobDescriptionComparisonChange> Compare(
        JobDescriptionComparisonSnapshot previous,
        JobDescriptionComparisonSnapshot current)
    {
        var changes = new List<JobDescriptionComparisonChange>();
        AddScalarChange(changes, "personName", previous.PersonName, current.PersonName);
        AddScalarChange(changes, "personnelCode", previous.PersonnelCode, current.PersonnelCode);
        AddScalarChange(changes, "education", previous.Education, current.Education);
        AddScalarChange(changes, "fieldOfStudy", previous.FieldOfStudy, current.FieldOfStudy);
        AddScalarChange(changes, "minimumExperience", previous.MinimumExperience, current.MinimumExperience);
        AddScalarChange(changes, "workflowStatus", previous.WorkflowStatus, current.WorkflowStatus);
        AddScalarChange(changes, "qualityStatus", previous.QualityStatus, current.QualityStatus);

        foreach (var skillId in previous.SkillIds.Except(current.SkillIds))
        {
            changes.Add(new($"skill:{skillId}", "removed", skillId.ToString(CultureInfo.InvariantCulture), null));
        }

        foreach (var skillId in current.SkillIds.Except(previous.SkillIds))
        {
            changes.Add(new($"skill:{skillId}", "added", null, skillId.ToString(CultureInfo.InvariantCulture)));
        }

        var previousTasks = previous.Tasks.ToDictionary(task => task.TaskCatalogItemId);
        var currentTasks = current.Tasks.ToDictionary(task => task.TaskCatalogItemId);
        foreach (var taskId in previousTasks.Keys.Except(currentTasks.Keys))
        {
            changes.Add(new($"task:{taskId}", "removed", previousTasks[taskId].Title, null));
        }

        foreach (var taskId in currentTasks.Keys.Except(previousTasks.Keys))
        {
            changes.Add(new($"task:{taskId}", "added", null, currentTasks[taskId].Title));
        }

        foreach (var taskId in previousTasks.Keys.Intersect(currentTasks.Keys))
        {
            var before = previousTasks[taskId];
            var after = currentTasks[taskId];
            AddScalarChange(changes, $"task:{taskId}/title", before.Title, after.Title);
            AddScalarChange(changes, $"task:{taskId}/description", before.Description, after.Description);
            AddScalarChange(changes, $"task:{taskId}/startDate", Format(before.StartDate), Format(after.StartDate));
            AddScalarChange(changes, $"task:{taskId}/endDate", Format(before.EndDate), Format(after.EndDate));
            AddScalarChange(changes, $"task:{taskId}/weeklyHours", Format(before.WeeklyHours), Format(after.WeeklyHours));
        }

        return changes;
    }

    private static void AddScalarChange(
        ICollection<JobDescriptionComparisonChange> changes,
        string field,
        string? before,
        string? after)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new(field, "changed", before, after));
        }
    }

    private static JobDescriptionComparisonSnapshot Snapshot(JobDescriptionVersion version) => new(
        version.Id,
        version.PersonName,
        version.DepartmentId,
        version.PersonnelCode,
        version.Education,
        version.FieldOfStudy,
        version.MinimumExperience,
        version.SkillIds.OrderBy(id => id).ToArray(),
        version.Tasks
            .OrderBy(task => task.SortOrder)
            .Select(task => new JobDescriptionComparisonTaskSnapshot(
                task.TaskCatalogItemId,
                task.Title,
                task.Description,
                task.StartDate,
                task.EndDate,
                task.SortOrder,
                task.WeeklyHours))
            .ToArray(),
        version.WorkflowStatus.ToString(),
        version.QualityStatus.ToString(),
        version.UpdatedAt);

    private static string? Format(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? Format(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
}
