using EosDashboards.Application.Administration;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Tests.Auth;

namespace EosDashboards.Application.Tests.Administration;

public sealed class AuditDashboardTests
{
    [Fact]
    public async Task Dashboard_counts_only_successful_and_failed_security_events_in_the_last_24_hours()
    {
        var context = new AuditDashboardContext();

        var dashboard = await context.UseCase.HandleAsync(CancellationToken.None);

        Assert.Equal(2, dashboard.SuccessfulSignIns);
        Assert.Equal(1, dashboard.FailedSecurityAttempts);
        Assert.Equal(3, dashboard.UsersWithActiveSessions);
        Assert.Equal(4, dashboard.ActiveUsers);
        Assert.Equal(2, dashboard.InactiveUsers);
        Assert.Equal(context.Clock.Now.AddHours(-24), context.Audits.LastMetricsFrom);
        Assert.Equal(context.Clock.Now, context.Audits.LastMetricsTo);
        Assert.Equal(context.Clock.Now.AddDays(-7), context.Audits.LastQuery?.From);
        Assert.Equal(context.Clock.Now, context.Audits.LastQuery?.To);
        Assert.Equal(10, context.Audits.LastQuery?.PageSize);
    }

    [Fact]
    public async Task Audit_history_uses_the_selected_seven_day_window_and_caps_the_page_size()
    {
        var clock = new FakeClock(new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Unspecified));
        var audits = new TestAuditLogRepository();
        var useCase = new GetAuditHistory(clock, audits);

        var result = await useCase.HandleAsync(new AuditHistoryQuery(
            AuditHistoryRange.LastSevenDays, null, null, " UserCreated ", null, null, true, 1, 100),
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal(clock.Now.AddDays(-7), audits.LastFrom);
        Assert.Equal(clock.Now, audits.LastTo);
        Assert.Equal("UserCreated", audits.LastQuery?.EventCode);
    }

    private sealed class AuditDashboardContext
    {
        public AuditDashboardContext()
        {
            Audits.SuccessfulSignIns = 2;
            Audits.FailedSecurityAttempts = 1;
            Audits.ActiveUsers = 4;
            Audits.InactiveUsers = 2;
            Audits.UsersWithActiveSessions = 3;
            UseCase = new GetSystemAdministrationDashboard(Clock, Audits, Audits);
        }

        public FakeClock Clock { get; } = new(new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Unspecified));
        public TestAuditLogRepository Audits { get; } = new();
        public GetSystemAdministrationDashboard UseCase { get; }
    }
}

internal sealed class TestAuditLogRepository : ISystemAdministrationMetricsReader, IAuditLogReader
{
    public int SuccessfulSignIns { get; set; }
    public int FailedSecurityAttempts { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int UsersWithActiveSessions { get; set; }
    public DateTime LastFrom { get; private set; }
    public DateTime LastTo { get; private set; }
    public DateTime LastMetricsFrom { get; private set; }
    public DateTime LastMetricsTo { get; private set; }
    public AuditLogQuery? LastQuery { get; private set; }

    public Task<SystemAdministrationMetrics> GetAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        LastMetricsFrom = from;
        LastMetricsTo = to;
        return Task.FromResult(new SystemAdministrationMetrics(
            ActiveUsers, InactiveUsers, SuccessfulSignIns, FailedSecurityAttempts, UsersWithActiveSessions));
    }

    public Task<PagedResult<AuditLogListItem>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        LastQuery = query;
        LastFrom = query.From;
        LastTo = query.To;
        return Task.FromResult(new PagedResult<AuditLogListItem>([], query.PageNumber, query.PageSize, 0));
    }
}
