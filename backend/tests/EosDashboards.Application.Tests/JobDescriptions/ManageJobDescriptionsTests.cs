using EosDashboards.Application.Abstractions;
using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class ManageJobDescriptionsTests
{
    [Fact]
    public async Task Manager_can_create_a_draft_and_send_it_to_human_resources()
    {
        var repository = new TestRepository();
        var manager = new ManageJobDescriptions(
            new TestClock(), repository, new TestScope(), new TestCatalog(), new TestGenerator(), new TestUnitOfWork());

        var result = await manager.CreateAsync(7, new CreateJobDescriptionCommand(
            "علی نمونه", 1, "P-1", "لیسانس", "نرم افزار", "۳ سال", [20],
            [new JobDescriptionTaskInput(10, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1, 40)]),
            CancellationToken.None);

        Assert.Equal(JobDescriptionOperationStatus.Succeeded, result.Status);
        Assert.Equal(JobDescriptionWorkflowStatus.PendingDepartmentApproval, result.Version!.WorkflowStatus);

        var approval = await manager.ApproveByDepartmentManagerAsync(7, result.Version.Id, CancellationToken.None);

        Assert.Equal(JobDescriptionOperationStatus.Succeeded, approval.Status);
        Assert.Equal(JobDescriptionWorkflowStatus.UnderHumanResourcesReview, approval.Version!.WorkflowStatus);
    }

    [Fact]
    public async Task Manager_cannot_create_a_job_description_without_personnel_code()
    {
        var manager = new ManageJobDescriptions(
            new TestClock(), new TestRepository(), new TestScope(), new TestCatalog(), new TestGenerator(), new TestUnitOfWork());

        var result = await manager.CreateAsync(7, new CreateJobDescriptionCommand(
            "علی نمونه", 1, "", "لیسانس", "نرم افزار", "۳ سال", [20],
            [new JobDescriptionTaskInput(10, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1)]),
            CancellationToken.None);

        Assert.Equal(JobDescriptionOperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Human_resources_actions_require_the_human_resources_role()
    {
        var repository = new TestRepository();
        var manager = new ManageJobDescriptions(
            new TestClock(), repository, new TestScope { CanReview = false }, new TestCatalog(), new TestGenerator(), new TestUnitOfWork());
        var version = JobDescriptionVersion.Create("علی نمونه", 1, "P-1", "لیسانس", "نرم افزار", "۳ سال", [20],
            [JobDescriptionTask.Create(10, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1, 40)],
            new DateTime(2026, 9, 4));
        version.ApproveByDepartmentManager(new DateTime(2026, 9, 4));
        repository.Version = version;

        var result = await manager.ApproveByHumanResourcesAsync(8, 1, CancellationToken.None);

        Assert.Equal(JobDescriptionOperationStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Manager_cannot_send_an_incomplete_version_and_receives_a_data_completion_result()
    {
        var repository = new TestRepository
        {
            Version = JobDescriptionVersion.Create(
                "علی نمونه", 1, null, "لیسانس", "نرم افزار", "۳ سال", [20],
                [JobDescriptionTask.Create(10, "توسعه نرم افزار", "شرح", new DateOnly(2026, 9, 1), null, 1, 40)],
                new DateTime(2026, 9, 4)),
        };
        var manager = new ManageJobDescriptions(
            new TestClock(), repository, new TestScope(), new TestCatalog(), new TestGenerator(), new TestUnitOfWork());

        var result = await manager.ApproveByDepartmentManagerAsync(7, 1, CancellationToken.None);

        Assert.Equal(JobDescriptionOperationStatus.Incomplete, result.Status);
        Assert.Equal(JobDescriptionWorkflowStatus.PendingDataCompletion, result.Version!.WorkflowStatus);
    }

    private sealed class TestClock : IClock
    {
        public DateTime Now => new(2026, 9, 4, 10, 0, 0);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        public Task ExecuteSerializedTransactionAsync(string operationKey, Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);
    }

    private sealed class TestGenerator : IJobDescriptionWorkbookGenerator
    {
        public byte[] Generate(JobDescriptionVersion version, DateOnly asOf) => [1];
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public bool CanReview { get; init; } = true;
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([1]);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(actorUserId == 7 && departmentId == 1);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(CanReview);
    }

    private sealed class TestCatalog : IJobDescriptionCatalogReader
    {
        public Task<bool> AreValidSelectionsAsync(long departmentId, IReadOnlyCollection<long> skillIds, IReadOnlyCollection<long> taskCatalogItemIds, CancellationToken cancellationToken) => Task.FromResult(departmentId == 1 && skillIds.Contains(20) && taskCatalogItemIds.Contains(10));
        public Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(IReadOnlyCollection<long> departmentIds, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillCatalogListItem>>([]);
        public Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(IReadOnlyCollection<long> departmentIds, long? departmentId, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TaskCatalogListItem>>([]);
        public Task<SkillCatalogItem?> FindSkillByNameAsync(long? departmentId, string name, long? excludingId, CancellationToken cancellationToken) => Task.FromResult<SkillCatalogItem?>(null);
        public Task<TaskCatalogItem?> FindTaskByTitleAsync(long departmentId, string title, long? excludingId, CancellationToken cancellationToken) => Task.FromResult<TaskCatalogItem?>(null);
        public Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<TaskCatalogItem?>(null);
        public Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<SkillCatalogItem?>(null);
        public Task<IReadOnlyCollection<long>> GetSkillUsageDepartmentIdsAsync(long skillId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public void AddSkill(SkillCatalogItem skill) { }
        public void AddTask(TaskCatalogItem task) { }
    }

    private sealed class TestRepository : IJobDescriptionRepository
    {
        public JobDescriptionVersion? Version { get; set; }
        public Task<JobDescriptionVersion?> GetForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult(Version);
        public Task<IReadOnlyList<JobDescriptionListItem>> ListAsync(IReadOnlyCollection<long> departmentIds, long? departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobDescriptionListItem>>([]);
        public Task<IReadOnlyList<JobDescriptionListItem>> ListForHumanResourcesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobDescriptionListItem>>([]);
        public void AddRecord(JobDescriptionRecord record) => SetId(record, 1);
        public void AddVersion(JobDescriptionVersion version) { SetId(version, 1); Version = version; }
        private static void SetId<T>(T entity, long id) => typeof(T).GetProperty(nameof(JobDescriptionVersion.Id))!.SetValue(entity, id);
    }
}
