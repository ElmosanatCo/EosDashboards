using EosDashboards.Application.Administration;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class AuditLogReader(EosDashboardDbContext context) : IAuditLogReader
{
    public async Task<PagedResult<AuditLogListItem>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var rows = context.AuditLogs.AsNoTracking().Where(audit =>
            audit.OccurredAt >= query.From && audit.OccurredAt < query.To &&
            (query.EventCode == null || audit.EventCode == query.EventCode) &&
            (query.ActorUserId == null || audit.ActorUserId == query.ActorUserId) &&
            (query.SubjectUserId == null || audit.SubjectUserId == query.SubjectUserId) &&
            (query.Succeeded == null || audit.Succeeded == query.Succeeded));
        var total = await rows.LongCountAsync(cancellationToken);
        var items = await (from audit in rows
                           join actor in context.Users.AsNoTracking() on audit.ActorUserId equals actor.Id into actors
                           from actor in actors.DefaultIfEmpty()
                           join subject in context.Users.AsNoTracking() on audit.SubjectUserId equals subject.Id into subjects
                           from subject in subjects.DefaultIfEmpty()
                           orderby audit.OccurredAt descending, audit.Id descending
                           select new AuditLogListItem(audit.Id, audit.OccurredAt, audit.EventCode, audit.Succeeded,
                               audit.ActorUserId, actor == null ? null : actor.FirstName + " " + actor.LastName,
                               audit.SubjectUserId, subject == null ? null : subject.FirstName + " " + subject.LastName,
                               audit.ClientIpAddress, audit.ClientDeviceKind))
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResult<AuditLogListItem>(items, query.PageNumber, query.PageSize, total);
    }
}
