using System.Text.Json;
using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class AuditWriter(
    EosDashboardDbContext context,
    IClock clock,
    ICorrelationContext? correlationContext = null) : IAuditWriter
{
    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeMetadata = record.SafeMetadata is null
            ? null
            : JsonSerializer.Serialize(record.SafeMetadata);

        context.AuditLogs.Add(AuditLog.Create(
            record.ActorUserId,
            record.SubjectUserId,
            record.EventCode,
            clock.Now,
            record.Succeeded,
            record.TraceId,
            safeMetadata,
            record.ClientIpAddress ?? correlationContext?.ClientIpAddress,
            record.ClientDeviceKind ?? correlationContext?.ClientDeviceKind));

        return Task.CompletedTask;
    }
}
