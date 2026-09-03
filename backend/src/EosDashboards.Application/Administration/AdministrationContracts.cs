namespace EosDashboards.Application.Administration;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount);

public sealed record AuditLogListItem(
    long Id,
    DateTime OccurredAt,
    string EventCode,
    bool Succeeded,
    long? ActorUserId,
    string? ActorDisplayName,
    long? SubjectUserId,
    string? SubjectDisplayName);

public sealed record AuditLogQuery(
    DateTime From,
    DateTime To,
    string? EventCode,
    long? ActorUserId,
    long? SubjectUserId,
    bool? Succeeded,
    int PageNumber,
    int PageSize);

public interface IAuditLogReader
{
    Task<PagedResult<AuditLogListItem>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
