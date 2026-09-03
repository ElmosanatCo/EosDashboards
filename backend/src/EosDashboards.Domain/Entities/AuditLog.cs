namespace EosDashboards.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog(
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        DateTimeOffset occurredAtUtc,
        bool succeeded,
        string traceId,
        string? safeMetadata)
    {
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        EventCode = eventCode;
        OccurredAtUtc = occurredAtUtc;
        Succeeded = succeeded;
        TraceId = traceId;
        SafeMetadata = safeMetadata;
    }

    public long Id { get; private set; }

    public long? ActorUserId { get; private set; }

    public long? SubjectUserId { get; private set; }

    public string EventCode { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public bool Succeeded { get; private set; }

    public string TraceId { get; private set; }

    public string? SafeMetadata { get; private set; }

    public static AuditLog Create(
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        DateTimeOffset occurredAtUtc,
        bool succeeded,
        string traceId,
        string? safeMetadata)
    {
        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actorUserId));
        }

        if (subjectUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subjectUserId));
        }

        if (string.IsNullOrWhiteSpace(eventCode))
        {
            throw new ArgumentException("An event code is required.", nameof(eventCode));
        }

        if (string.IsNullOrWhiteSpace(traceId))
        {
            throw new ArgumentException("A trace identifier is required.", nameof(traceId));
        }

        return new AuditLog(
            actorUserId,
            subjectUserId,
            eventCode,
            occurredAtUtc.ToUniversalTime(),
            succeeded,
            traceId,
            safeMetadata);
    }
}
