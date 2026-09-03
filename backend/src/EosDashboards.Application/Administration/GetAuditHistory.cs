using EosDashboards.Application.Abstractions;

namespace EosDashboards.Application.Administration;

public enum AuditHistoryRange
{
    LastSevenDays,
    LastThirtyDays,
    Custom,
}

public sealed record AuditHistoryQuery(
    AuditHistoryRange Range,
    DateTime? From,
    DateTime? To,
    string? EventCode,
    long? ActorUserId,
    long? SubjectUserId,
    bool? Succeeded,
    int PageNumber = 1,
    int PageSize = 50);

public sealed record AuditHistoryPage(bool IsValid, PagedResult<AuditLogListItem>? Value);

public sealed class GetAuditHistory(IClock clock, IAuditLogReader auditLogs)
{
    public async Task<AuditHistoryPage> HandleAsync(AuditHistoryQuery query, CancellationToken cancellationToken)
    {
        if (query is null || query.PageNumber < 1 || query.PageSize is < 1 or > 100 ||
            query.ActorUserId <= 0 || query.SubjectUserId <= 0)
        {
            return new AuditHistoryPage(false, null);
        }

        var to = clock.Now;
        DateTime from;
        switch (query.Range)
        {
            case AuditHistoryRange.LastSevenDays:
                from = to.AddDays(-7);
                break;
            case AuditHistoryRange.LastThirtyDays:
                from = to.AddDays(-30);
                break;
            case AuditHistoryRange.Custom when query.From is { } customFrom && query.To is { } customTo && customFrom < customTo:
                from = customFrom;
                to = customTo;
                break;
            default:
                return new AuditHistoryPage(false, null);
        }

        return new AuditHistoryPage(true, await auditLogs.QueryAsync(new AuditLogQuery(
            from, to, NormalizeEventCode(query.EventCode), query.ActorUserId, query.SubjectUserId,
            query.Succeeded, query.PageNumber, query.PageSize), cancellationToken));
    }

    private static string? NormalizeEventCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
