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

    private static ManageCatalog CreateManager(TestCatalog catalog) => new(
        new TestClock(),
        new TestScope(),
        catalog,
        catalog,
        new TestUnitOfWork());

    private static readonly DateTime Now = new(2026, 9, 4, 12, 0, 0);

    private sealed class TestClock : IClock
    {
        public DateTime Now => ManageCatalogTests.Now;
    }

    private sealed class TestScope : IJobDescriptionScope
    {
        public Task<IReadOnlyList<long>> GetManagedDepartmentIdsAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<long>>([1]);
        public Task<bool> CanManageDepartmentAsync(long actorUserId, long departmentId, CancellationToken cancellationToken) => Task.FromResult(actorUserId == 7 && departmentId == 1);
        public Task<bool> CanReviewAsHumanResourcesAsync(long actorUserId, CancellationToken cancellationToken) => Task.FromResult(false);
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
        public List<SkillCatalogItem> AddedSkills { get; } = [];

        public Task<SkillCatalogItem?> FindSkillByNameAsync(long? departmentId, string name, long? excludingId, CancellationToken cancellationToken) => Task.FromResult(DuplicateSkill);
        public Task<TaskCatalogItem?> FindTaskByTitleAsync(long departmentId, string title, long? excludingId, CancellationToken cancellationToken) => Task.FromResult<TaskCatalogItem?>(null);
        public Task<SkillCatalogItem?> GetSkillForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult(SkillForUpdate);
        public Task<TaskCatalogItem?> GetTaskForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<TaskCatalogItem?>(null);
        public Task<IReadOnlyCollection<long>> GetSkillUsageDepartmentIdsAsync(long skillId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task<bool> AreSkillsAvailableAsync(long departmentId, IReadOnlyCollection<long> skillIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> AreValidSelectionsAsync(long departmentId, IReadOnlyCollection<long> skillIds, IReadOnlyCollection<long> taskCatalogItemIds, CancellationToken cancellationToken) => Task.FromResult(true);
        public void AddSkill(SkillCatalogItem skill) => AddedSkills.Add(skill);
        public void AddTask(TaskCatalogItem task) { }
        public Task<IReadOnlyList<SkillCatalogListItem>> ListSkillsAsync(IReadOnlyCollection<long> departmentIds, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillCatalogListItem>>([]);
        public Task<IReadOnlyList<TaskCatalogListItem>> ListTasksAsync(IReadOnlyCollection<long> departmentIds, long? departmentId, bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TaskCatalogListItem>>([]);
        public Task<IReadOnlyList<SkillCatalogListItem>> ListPublicSkillsAsync(bool includeInactive, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillCatalogListItem>>([]);
        public Task<SkillCatalogItem?> GetPublicSkillForUpdateAsync(long id, CancellationToken cancellationToken) => Task.FromResult<SkillCatalogItem?>(null);
    }
}
