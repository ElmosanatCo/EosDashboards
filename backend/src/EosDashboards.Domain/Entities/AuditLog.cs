namespace EosDashboards.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog(
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        DateTime occurredAt,
        bool succeeded,
        string traceId,
        string? safeMetadata)
    {
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        EventCode = eventCode;
        OccurredAt = occurredAt;
        Succeeded = succeeded;
        TraceId = traceId;
        SafeMetadata = safeMetadata;
    }

    public long Id { get; private set; }

    public long? ActorUserId { get; private set; }

    public long? SubjectUserId { get; private set; }

    public string EventCode { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public bool Succeeded { get; private set; }

    public string TraceId { get; private set; }

    public string? SafeMetadata { get; private set; }

    public static AuditLog Create(
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        DateTime occurredAt,
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
            occurredAt,
            succeeded,
            traceId,
            safeMetadata);
    }
}
