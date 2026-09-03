using EosDashboards.Application.Auth;

namespace EosDashboards.Application.Abstractions;

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken);
}
