using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Preferences;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.Preferences;

public sealed class UserPreferenceTests
{
    [Fact]
    public async Task Read_returns_defaults_and_update_upserts_the_current_user_only()
    {
        var repository = new PreferenceRepository();
        var audit = new AuditWriter();
        var read = new GetMyPreferences(repository);
        var update = new UpdateMyPreferences(
            new Clock(), new Correlation(), repository, audit, new UnitOfWork());

        Assert.Equal(new UserPreferenceDto("system", "navyTeal", false),
            await read.HandleAsync(7, CancellationToken.None));
        var saved = await update.HandleAsync(
            7,
            new UpdateMyPreferencesCommand("dark", "navyTeal", true),
            CancellationToken.None);

        Assert.Equal(new UserPreferenceDto("dark", "navyTeal", true), saved);
        Assert.Equal(7, Assert.Single(repository.Items).UserId);
        Assert.Equal("UserPreferenceChanged", Assert.Single(audit.Records).EventCode);
    }

    [Fact]
    public async Task Update_rejects_values_outside_the_allowlist()
    {
        var repository = new PreferenceRepository();
        var useCase = new UpdateMyPreferences(
            new Clock(), new Correlation(), repository, new AuditWriter(), new UnitOfWork());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.HandleAsync(
            7,
            new UpdateMyPreferencesCommand("automatic", "navyTeal", false),
            CancellationToken.None));
        Assert.Empty(repository.Items);
    }

    private sealed class PreferenceRepository : IUserPreferenceRepository
    {
        public List<UserPreference> Items { get; } = [];
        public Task<UserPreference?> FindByUserIdAsync(long userId, CancellationToken _) =>
            Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId));
        public Task<UserPreference?> GetForUpdateAsync(long userId, CancellationToken _) =>
            FindByUserIdAsync(userId, _);
        public void Add(UserPreference preference) => Items.Add(preference);
    }

    private sealed class Clock : IClock { public DateTimeOffset UtcNow => new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero); }
    private sealed class Correlation : ICorrelationContext { public string TraceId => "preference-test"; }
    private sealed class AuditWriter : IAuditWriter
    {
        public List<AuditRecord> Records { get; } = [];
        public Task WriteAsync(AuditRecord record, CancellationToken _) { Records.Add(record); return Task.CompletedTask; }
    }
    private sealed class UnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken _) => Task.FromResult(1);
        public Task ExecuteSerializedTransactionAsync(string _, Func<CancellationToken, Task> operation, CancellationToken token) => operation(token);
    }
}
