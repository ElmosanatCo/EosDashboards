using EosDashboards.Application.JobDescriptions;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class GetJobDescriptionReviewWarningsTests
{
    [Fact]
    public async Task Department_manager_receives_review_warnings_for_managed_departments()
    {
        var reader = new TestReader();
        var query = new GetJobDescriptionReviewWarnings(new TestScope(), reader);

        var result = await query.HandleAsync(7, CancellationToken.None);

        var warning = Assert.Single(result!);
        Assert.Equal(4, warning.VersionId);
        Assert.Equal("توسعه نرم افزار", warning.TaskTitle);
        Assert.Equal("مدیریت پروژه", warning.MissingSkillName);
        Assert.Equal([1L], reader.RequestedDepartmentIds);
    }

    [Fact]
    public async Task Unscoped_actor_cannot_read_review_warnings()
    {
        var query = new GetJobDescriptionReviewWarnings(new TestScope { IsManager = false }, new TestReader());

        var result = await query.HandleAsync(8, CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public bool IsManager { get; init; } = true;
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<long>>(IsManager ? [1] : []);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(IsManager);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CanReviewAsChiefExecutiveAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class TestReader : IJobDescriptionReviewWarningReader
    {
        public IReadOnlyList<long>? RequestedDepartmentIds { get; private set; }

        public Task<IReadOnlyList<JobDescriptionReviewWarning>> ListAsync(
            IReadOnlyCollection<long>? departmentIds,
            CancellationToken cancellationToken)
        {
            RequestedDepartmentIds = departmentIds?.ToArray();
            return Task.FromResult<IReadOnlyList<JobDescriptionReviewWarning>>([
                new(4, 1, "نرم افزار", "علی نمونه", "توسعه نرم افزار", "مدیریت پروژه")
            ]);
        }
    }
}
