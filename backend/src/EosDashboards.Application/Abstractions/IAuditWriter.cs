namespace EosDashboards.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
}

public sealed record AuditRecord(
    long? ActorUserId,
    long? SubjectUserId,
    string EventCode,
    bool Succeeded,
    string TraceId,
    IReadOnlyDictionary<string, string>? SafeMetadata,
    string? ClientIpAddress = null,
    string? ClientDeviceKind = null);
