using Microsoft.Extensions.Options;

namespace EosDashboards.Infrastructure.Sms;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";
    public const string HttpClientName = "CompanySms";

    public string? Endpoint { get; init; }

    public TimeSpan Timeout { get; init; }
}

internal sealed class SmsOptionsValidator : IValidateOptions<SmsOptions>
{
    private static readonly TimeSpan MaximumTimeout = TimeSpan.FromSeconds(30);

    public ValidateOptionsResult Validate(string? name, SmsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) ||
            !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{nameof(SmsOptions)}.{nameof(SmsOptions.Endpoint)}");
        }

        if (options.Timeout <= TimeSpan.Zero || options.Timeout > MaximumTimeout)
        {
            failures.Add($"{nameof(SmsOptions)}.{nameof(SmsOptions.Timeout)}");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
