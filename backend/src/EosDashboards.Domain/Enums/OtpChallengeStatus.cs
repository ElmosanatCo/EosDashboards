namespace EosDashboards.Domain.Enums;

public enum OtpChallengeStatus
{
    Pending,
    Sent,
    SendFailed,
    Superseded,
    Consumed,
    Expired,
    Exhausted,
}
