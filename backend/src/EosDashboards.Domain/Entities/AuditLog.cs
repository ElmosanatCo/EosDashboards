using System.Net;

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
        string? safeMetadata,
        string? clientIpAddress,
        string? clientDeviceKind)
    {
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        EventCode = eventCode;
        OccurredAt = occurredAt;
        Succeeded = succeeded;
        TraceId = traceId;
        SafeMetadata = safeMetadata;
        ClientIpAddress = clientIpAddress;
        ClientDeviceKind = clientDeviceKind;
    }

    public long Id { get; private set; }

    public long? ActorUserId { get; private set; }

    public long? SubjectUserId { get; private set; }

    public string EventCode { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public bool Succeeded { get; private set; }

    public string TraceId { get; private set; }

    public string? SafeMetadata { get; private set; }

    public string? ClientIpAddress { get; private set; }

    public string? ClientDeviceKind { get; private set; }

    public static AuditLog Create(
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        DateTime occurredAt,
        bool succeeded,
        string traceId,
        string? safeMetadata,
        string? clientIpAddress = null,
        string? clientDeviceKind = null)
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

        var normalizedIpAddress = NormalizeIpAddress(clientIpAddress);
        var normalizedDeviceKind = NormalizeDeviceKind(clientDeviceKind);

        return new AuditLog(
            actorUserId,
            subjectUserId,
            eventCode,
            occurredAt,
            succeeded,
            traceId,
            safeMetadata,
            normalizedIpAddress,
            normalizedDeviceKind);
    }

    private static string? NormalizeIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > 45 || !IPAddress.TryParse(trimmed, out var address))
        {
            throw new ArgumentException("A valid IP address is required.", nameof(value));
        }

        return address.ToString();
    }

    private static string? NormalizeDeviceKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim() switch
        {
            "Desktop" or "Mobile" or "Tablet" or "Unknown" => value.Trim(),
            _ => throw new ArgumentException("An approved device kind is required.", nameof(value)),
        };
    }
}
