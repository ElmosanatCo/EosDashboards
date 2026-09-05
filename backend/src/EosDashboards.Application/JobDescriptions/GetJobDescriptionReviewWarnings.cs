namespace EosDashboards.Application.JobDescriptions;

public sealed class GetJobDescriptionReviewWarnings(
    IJobDescriptionScope scope,
    IJobDescriptionReviewWarningReader reader)
{
    public async Task<IReadOnlyList<JobDescriptionReviewWarning>?> HandleAsync(
        long actorUserId,
        CancellationToken cancellationToken)
    {
        if (await scope.CanReviewAsChiefExecutiveAsync(actorUserId, cancellationToken))
        {
            return await reader.ListAsync(null, cancellationToken);
        }

        var managedDepartmentIds = await scope.GetManagedDepartmentIdsAsync(actorUserId, cancellationToken);
        return managedDepartmentIds.Count == 0
            ? null
            : await reader.ListAsync(managedDepartmentIds, cancellationToken);
    }
}
