namespace EosDashboards.Application.JobDescriptions;

public sealed class GetHumanResourcesDashboard(
    IHumanResourcesDashboardReader reader,
    IJobDescriptionScope scope)
{
    public async Task<HumanResourcesDashboardResult?> HandleAsync(
        long actorUserId,
        long? selectedDepartmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await scope.CanReviewAsHumanResourcesAsync(actorUserId, cancellationToken))
        {
            return null;
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var departments = await reader.ListDepartmentsAsync(cancellationToken);
        if (selectedDepartmentId is not null &&
            departments.All(department => department.Id != selectedDepartmentId.Value))
        {
            return null;
        }

        return await reader.GetAsync(selectedDepartmentId, page, pageSize, cancellationToken);
    }
}
