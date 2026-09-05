using EosDashboards.Application.Abstractions;
using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.JobDescriptions;

public sealed class ManageCatalogTests
{
    [Fact]
    public async Task Creating_a_duplicate_skill_returns_a_duplicate_result_without_adding_it()
    {
        var existing = SkillCatalogItem.Create(1, "مدیریت پروژه", Now);
        var catalog = new TestCatalog { DuplicateSkill = existing };
        var manager = CreateManager(catalog);

        var result = await manager.CreateSkillAsync(
            7,
            new CreateSkillCommand(1, " مدیریت پروژه "),
            CancellationToken.None);

        Assert.Equal(CatalogOperationStatus.Duplicate, result.Status);
        Assert.Empty(catalog.AddedSkills);
    }

    [Fact]
    public async Task Creating_a_name_used_by_an_inactive_skill_returns_an_inactive_duplicate_result()
    {
        var existing = SkillCatalogItem.Create(1, "مدیریت پروژه", Now);
        existing.Deactivate(Now.AddMinutes(1));
        var catalog = new TestCatalog { DuplicateSkill = existing };
        var manager = CreateManager(catalog);

        var result = await manager.CreateSkillAsync(
            7,
            new CreateSkillCommand(1, "مدیریت پروژه"),
            CancellationToken.None);

        Assert.Equal(CatalogOperationStatus.InactiveDuplicate, result.Status);
        Assert.Empty(catalog.AddedSkills);
    }

    [Fact]
    public async Task A_department_manager_can_activate_a_skill_in_scope()
    {
        var skill = SkillCatalogItem.Create(1, "مدیریت پروژه", Now);
        skill.Deactivate(Now.AddMinutes(1));
        var catalog = new TestCatalog { SkillForUpdate = skill };
        var manager = CreateManager(catalog);

        var result = await manager.ActivateDepartmentSkillAsync(7, skill.Id, CancellationToken.None);

        Assert.Equal(CatalogOperationStatus.Succeeded, result.Status);
        Assert.True(skill.IsActive);
    }

    [Fact]
    public async Task Human_resources_can_merge_public_skills_and_deactivate_the_source()
    {
        var source = SkillCatalogItem.CreatePublic(1, "مهارت قدیمی", Now);
        var target = SkillCatalogItem.CreatePublic(1, "مهارت باقی‌مانده", Now);
        SetId(source, 11);
        SetId(target, 12);
        var catalog = new TestCatalog { MergePair = new(source, target) };
        var audit = new TestAuditWriter();
        var manager = new ManageCatalog(
            new TestClock(),
            new TestScope { CanReview = true },
            catalog,
            catalog,
            new TestRepository(),
            new TestUnitOfWork(),
            audit,
            new TestCorrelationContext());

        var result = await manager.MergePublicSkillAsync(8, source.Id, target.Id, CancellationToken.None);

        Assert.Equal(CatalogOperationStatus.Succeeded, result.Status);
        Assert.False(source.IsActive);
        Assert.True(catalog.MergeCalled);
        Assert.Contains(audit.Records, record =>
            record.EventCode == "job-description.public-skill-merged" &&
            record.SafeMetadata!["survivingSkillName"] == "مهارت باقی‌مانده");
    }

    [Fact]
    public async Task Updating_task_required_skills_revalidates_active_job_descriptions()
    {
        var task = TaskCatalogItem.Create(1, "پاسخگویی", false, Now);
        SetId(task, 10);
        var catalog = new TestCatalog { TaskForUpdate = task };
        var repository = new TestRepository();
        var manager = CreateManager(catalog, repository);

        var result = await manager.SetRequiredSkillsAsync(
            7,
            new SetTaskRequiredSkillsCommand(task.Id, [20]),
            CancellationToken.None);

        Assert.Equal(CatalogOperationStatus.Succeeded, result.Status);
        Assert.Equal(Now, repository.RevalidationAt);
        Assert.Equal(task.Id, repository.RevalidatedTaskId);
    }

    private static ManageCatalog CreateManager(TestCatalog catalog, TestRepository? repository = null) => new(
        new TestClock(),
        new TestScope(),
        catalog,
        catalog,
        repository ?? new TestRepository(),
        new TestUnitOfWork(),
        new TestAuditWriter(),
        new TestCorrelationContext());

    private static readonly DateTime Now = new(2026, 9, 4, 12, 0, 0);

    private sealed class TestClock : IClock
    {
        public DateTime Now => ManageCatalogTests.Now;
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public bool CanReview { get; init; }
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([1]);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(actorUserId == 7 && departmentId == 1);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(CanReview);
        public Task<bool> CanReviewAsChiefExecutiveAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        public Task ExecuteSerializedTransactionAsync(string operationKey, Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);
    }

    private sealed class TestCatalog : IJobDescriptionCatalogReader, IHumanResourcesCatalogReader
    {
        public SkillCatalogItem? DuplicateSkill { get; init; }
        public SkillCatalogItem? SkillForUpdate { get; init; }
        public TaskCatalogItem? TaskForUpdate { get; init; }
        public List<SkillCatalogItem> AddedSkills { get; } = [];
        public (SkillCatalogItem Source, SkillCatalogItem Target)? MergePair { get; init; }
        public bool MergeCalled { get; private set; }

        public Task<IReadOnlyList<string>> GetSkillNamesAsync(IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyDictionary<long, string>> GetSkillNameMapAsync(IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<long, string>>(new Dictionary<long, string>());
        public Task<SkillCatalogItem?> FindSkillByNameAsync(long? departmentId, string name, long? excludingId, CancellationToken cancellationToken) => Task.FromResult(DuplicateSkill);
        public Task<TaskCatalogItem?> FindTaskByTitleAsync(long departmentId, string title, long? excludingId, CancellationToken cancellationToken) => Task.FromResult<TaskCatalogItem?>(null);
        public Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult(SkillForUpdate);
        public Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult(TaskForUpdate);
        public Task<IReadOnlyCollection<long>> GetSkillUsageDepartmentIdsAsync(long skillId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> AreValidSelectionsAsync(long departmentId, IReadOnlyCollection<long> skillIds, IReadOnlyCollection<long> taskCatalogItemIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public void AddSkill(SkillCatalogItem skill) => AddedSkills.Add(skill);
        public void AddTask(TaskCatalogItem task) { }
        public Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(IReadOnlyCollection<long> departmentIds, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillCatalogListItem>>([]);
        public Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(IReadOnlyCollection<long> departmentIds, long? departmentId, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TaskCatalogListItem>>([]);
        public Task<IReadOnlyList<SkillCatalogListItem>> ListPublicSkillsAsync(bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillCatalogListItem>>([]);
        public Task<SkillCatalogItem?> GetPublicSkillForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<SkillCatalogItem?>(null);
        public Task<(SkillCatalogItem Source, SkillCatalogItem Target)?> GetPublicSkillPairForMergeAsync(long sourceSkillId, long survivingSkillId, CancellationToken cancellationToken) => Task.FromResult(MergePair);
        public Task MergePublicSkillReferencesAsync(long sourceSkillId, long survivingSkillId, CancellationToken cancellationToken)
        {
            MergeCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestRepository : IJobDescriptionRepository
    {
        public long? RevalidatedTaskId { get; private set; }
        public DateTime? RevalidationAt { get; private set; }

        public Task<JobDescriptionVersion?> GetForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<JobDescriptionVersion?>(null);
        public Task DeleteVersionAsync(JobDescriptionVersion version, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<JobDescriptionListItem>> ListAsync(IReadOnlyCollection<long> departmentIds, long? departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobDescriptionListItem>>([]);
        public Task<IReadOnlyList<JobDescriptionListItem>> ListForHumanResourcesAsync(long? departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobDescriptionListItem>>([]);
        public Task<IReadOnlyList<JobDescriptionListItem>> ListApprovedForHumanResourcesAsync(long? departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobDescriptionListItem>>([]);
        public Task RevalidateActiveJobDescriptionsAsync(long taskId, DateTime occurredAt, CancellationToken cancellationToken)
        {
            RevalidatedTaskId = taskId;
            RevalidationAt = occurredAt;
            return Task.CompletedTask;
        }
        public void AddRecord(JobDescriptionRecord record) { }
        public void AddVersion(JobDescriptionVersion version) { }
    }

    private sealed class TestAuditWriter : IAuditWriter
    {
        public List<AuditRecord> Records { get; } = [];
        public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class TestCorrelationContext : ICorrelationContext
    {
        public string TraceId => "trace-merge";
    }

    private static void SetId<T>(T entity, long id) => typeof(T).GetProperty("Id")!.SetValue(entity, id);
}
