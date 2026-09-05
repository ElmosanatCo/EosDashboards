using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class CompareJobDescriptionVersionsTests
{
    [Fact]
    public async Task Compares_current_version_with_the_immediately_previous_version()
    {
        var previous = CreateVersion("نام قبلی", "توضیح قبلی", 2026, 9, 1);
        var current = CreateVersion("نام جدید", "توضیح جدید", 2026, 9, 2);
        var reader = new TestComparisonReader(current, previous);
        var comparer = new CompareJobDescriptionVersions(reader, new TestScope());

        var result = await comparer.HandleAsync(8, 2, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.CurrentVersionId);
        Assert.Equal(1, result.PreviousVersionId);
        Assert.Contains(result.Changes, change => change.Field == "personName" && change.Kind == "changed");
        Assert.Contains(result.Changes, change => change.Field == "task:20/description" && change.Kind == "changed");
    }

    [Fact]
    public async Task Returns_no_previous_version_state_without_failing()
    {
        var current = CreateVersion("نام جدید", "توضیح جدید", 2026, 9, 2);
        var comparer = new CompareJobDescriptionVersions(
            new TestComparisonReader(current, null), new TestScope());

        var result = await comparer.HandleAsync(8, 2, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.PreviousVersionId);
        Assert.Empty(result.Changes);
    }

    [Fact]
    public async Task Comparison_requires_human_resources_access()
    {
        var comparer = new CompareJobDescriptionVersions(
            new TestComparisonReader(CreateVersion("نام", "شرح", 2026, 9, 2), null),
            new TestScope { CanReview = false });

        var result = await comparer.HandleAsync(8, 2, CancellationToken.None);

        Assert.Null(result);
    }

    private static JobDescriptionVersion CreateVersion(string personName, string description, int year, int month, int day)
    {
        var version = JobDescriptionVersion.Create(
            personName,
            1,
            "P-1",
            "لیسانس",
            "نرم افزار",
            "۳ سال",
            [10],
            [JobDescriptionTask.Create(20, "وظیفه", description, new DateOnly(2026, 9, 1), null, 1, 40)],
            new DateTime(year, month, day, 10, 0, 0));
        typeof(JobDescriptionVersion).GetProperty(nameof(JobDescriptionVersion.Id))!.SetValue(version, day == 1 ? 1 : 2);
        return version;
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public bool CanReview { get; init; } = true;
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([]);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(CanReview);
    }

    private sealed class TestComparisonReader(JobDescriptionVersion current, JobDescriptionVersion? previous) : IJobDescriptionComparisonReader
    {
        public Task<JobDescriptionComparisonVersions?> GetAsync(long versionId, CancellationToken cancellationToken) =>
            Task.FromResult<JobDescriptionComparisonVersions?>(versionId == current.Id ? new(current, previous) : null);
    }
}
