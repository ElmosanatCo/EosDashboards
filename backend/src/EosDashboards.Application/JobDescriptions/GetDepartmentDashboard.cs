using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.JobDescriptions;

public sealed record DepartmentDashboardMetrics(
    int PersonnelCount,
    int ActivePersonnelCount,
    int ArchivedPersonnelCount,
    int HealthyDescriptionCount,
    int IncompleteDescriptionCount,
    int PendingDataCompletionCount,
    int PendingDepartmentApprovalCount,
    int UnderHumanResourcesReviewCount,
    int ApprovedDescriptionCount,
    int RejectedDescriptionCount,
    int ActiveProjectCount,
    int PeopleWorkingOnActiveProjectsCount);

public interface IDepartmentDashboardReader
{
    Task<DepartmentDashboardMetrics> GetAsync(
        IReadOnlyCollection<long> departmentIds,
        DateOnly asOf,
        CancellationToken cancellationToken);
}

public sealed class GetDepartmentDashboard(
    IJobDescriptionScope scope,
    IDepartmentDashboardReader reader,
    EosDashboards.Application.Abstractions.IClock clock)
{
    public async Task<DepartmentDashboardMetrics?> HandleAsync(
        long actorUserId,
        long? selectedDepartmentId,
        CancellationToken cancellationToken)
    {
        var departmentIds = await scope.GetManagedDepartmentIdsAsync(actorUserId, cancellationToken);
        if (departmentIds.Count == 0 ||
            (selectedDepartmentId is not null && !departmentIds.Contains(selectedDepartmentId.Value)))
        {
            return null;
        }

        var selected = selectedDepartmentId is null ? departmentIds : [selectedDepartmentId.Value];
        return await reader.GetAsync(selected, DateOnly.FromDateTime(clock.Now), cancellationToken);
    }
}
