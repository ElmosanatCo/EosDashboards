namespace EosDashboards.Application.JobDescriptions;

public sealed class AnalyzeJobDescription(
    IJobDescriptionRepository repository,
    IJobDescriptionScope scope,
    IJobDescriptionAnalysisReader analysisReader)
{
    public async Task<IReadOnlyList<JobDescriptionQualityFinding>?> AnalyzeAsync(long actorUserId, long versionId, CancellationToken cancellationToken)
    {
        var version = await repository.GetForUpdateAsync(versionId, cancellationToken);
        if (version is null) return null;
        if (!await scope.CanManageDepartmentAsync(actorUserId, version.DepartmentId, cancellationToken) &&
            !await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
            return null;

        var taskCatalog = await analysisReader.GetTasksAsync(
            version.DepartmentId,
            version.Tasks.Select(task => task.TaskCatalogItemId).Distinct().ToArray(),
            cancellationToken);
        return JobDescriptionQualityAnalyzer.Analyze(version, taskCatalog);
    }
}
