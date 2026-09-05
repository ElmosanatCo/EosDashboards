using EosDashboards.Application.JobDescriptions;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class HumanResourcesDashboardTests
{
    [Fact]
    public async Task Uses_all_departments_when_no_department_is_selected()
    {
        var reader = new TestDashboardReader();
        var dashboard = new GetHumanResourcesDashboard(reader, new TestScope());

        var result = await dashboard.HandleAsync(8, null, 1, 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(reader.SelectedDepartmentId);
        Assert.Equal(2, result!.ChangeSummaries.Count);
    }

    [Fact]
    public async Task Rejects_a_department_outside_the_human_resources_dataset()
    {
        var dashboard = new GetHumanResourcesDashboard(new TestDashboardReader(), new TestScope());

        var result = await dashboard.HandleAsync(8, 99, 1, 20, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Requires_human_resources_access()
    {
        var dashboard = new GetHumanResourcesDashboard(new TestDashboardReader(), new TestScope { CanReview = false });

        var result = await dashboard.HandleAsync(8, null, 1, 20, CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public bool CanReview { get; init; } = true;
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([]);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(CanReview);
    }

    private sealed class TestDashboardReader : IHumanResourcesDashboardReader
    {
        public long? SelectedDepartmentId { get; private set; }

        public Task<IReadOnlyList<ManagedDepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ManagedDepartmentListItem>>([
                new(1, "نرم افزار", true),
                new(2, "منابع انسانی", false),
            ]);

        public Task<HumanResourcesDashboardResult> GetAsync(long? departmentId, int page, int pageSize, CancellationToken cancellationToken)
        {
            SelectedDepartmentId = departmentId;
            return Task.FromResult(new HumanResourcesDashboardResult(
                new HumanResourcesMetricSet(2, 2, 0, 2, 0, 1, 1, 0, 0, 0, 0, 0),
                [new HumanResourcesChangeSummary(1, "نرم افزار", 1, new DateTime(2026, 9, 2)), new HumanResourcesChangeSummary(2, "منابع انسانی", 1, new DateTime(2026, 9, 2))],
                [],
                2,
                page,
                pageSize));
        }
    }
}
