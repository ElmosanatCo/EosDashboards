using System.Net;
using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Infrastructure;
using EosDashboards.Infrastructure.Sms;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EosDashboards.IntegrationTests.Sms;

public sealed class SoapSmsSenderTests
{
    private const string Endpoint = "https://sms.test.example/CompanySms.asmx";
    private const string Message = "secret <message> & \"quote\"";
    private const string Mobile = "09123456789";

    [Fact]
    public async Task SendAsync_posts_one_escaped_soap_11_request_and_maps_true_result()
    {
        // Break caught: changing the SOAP 1.1 contract, request element order, or success mapping.
        var handler = new RecordingHandler(SoapResponse("true"));
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(true, null), result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Endpoint, request.Uri.AbsoluteUri);
        Assert.Equal("text/xml; charset=utf-8", request.ContentType);
        Assert.Equal("http://tempuri.org/SendSmsMessage", request.SoapAction);
        Assert.True(request.HasOrderedMessageAndMobile, "SOAP request must contain message followed by mobile.");
        Assert.True(request.SensitiveValuesRoundTrip, "SOAP request values must round trip without structural injection.");
    }

    [Fact]
    public async Task SendAsync_maps_false_provider_result_without_retrying()
    {
        // Break caught: treating a provider rejection as delivery success or issuing an unsafe retry.
        var handler = new RecordingHandler(SoapResponse("false"));
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "provider-rejected"), result);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("<broken")]
    [InlineData("<Envelope xmlns=\"http://schemas.xmlsoap.org/soap/envelope/\"><Body /></Envelope>")]
    [InlineData("<SendSmsMessageResult>not-a-boolean</SendSmsMessageResult>")]
    [InlineData("<!DOCTYPE a [ <!ENTITY xxe SYSTEM \"file:///not-read\"> ]><SendSmsMessageResult>&xxe;</SendSmsMessageResult>")]
    public async Task SendAsync_maps_unsafe_or_invalid_response_to_safe_failure(string responseBody)
    {
        // Break caught: accepting malformed/unsafe provider replies or leaking their contents through the result.
        var handler = new RecordingHandler(responseBody);
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "invalid-response"), result);
        Assert.True(HasOnlySafeResultMetadata(result), "SMS result metadata must be a known safe code.");
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_maps_non_success_status_to_safe_failure_without_retrying()
    {
        // Break caught: parsing an HTTP error as a successful delivery or retrying a non-idempotent send.
        var handler = new RecordingHandler("untrusted failure body", HttpStatusCode.BadGateway);
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "provider-unavailable"), result);
        Assert.True(HasOnlySafeResultMetadata(result), "SMS result metadata must be a known safe code.");
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_maps_ambiguous_transport_timeout_to_safe_failure_without_retrying()
    {
        // Break caught: surfacing ambiguous timeout cancellation to callers or sending a duplicate message.
        var handler = new RecordingHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SoapHttpResponse("true");
        });
        var sender = CreateSender(handler, TimeSpan.FromMilliseconds(50));

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "timeout"), result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_rejects_an_oversized_response_without_retrying()
    {
        // Break caught: accepting an unbounded provider response that can exhaust process memory.
        var handler = new RecordingHandler(OversizedSoapResponse());
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "invalid-response"), result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_times_out_when_response_headers_arrive_but_the_body_stalls()
    {
        // Break caught: enforcing timeout only until headers arrive, leaving a stalled body read unbounded.
        var handler = new RecordingHandler(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new SlowBodyContent(),
        }));
        var sender = CreateSender(handler, TimeSpan.FromMilliseconds(50));

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "timeout"), result);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><SendSmsMessageResult>true</SendSmsMessageResult></soap:Body></soap:Envelope><trailing")]
    [InlineData("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><SendSmsMessageResult>true</SendSmsMessageResult><SendSmsMessageResult>false</SendSmsMessageResult></soap:Body></soap:Envelope>")]
    public async Task SendAsync_rejects_malformed_trailing_xml_or_duplicate_results(string responseBody)
    {
        // Break caught: accepting an early valid result before fully validating the complete SOAP document.
        var handler = new RecordingHandler(responseBody);
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "invalid-response"), result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_rejects_a_success_result_followed_by_an_oversized_body()
    {
        // Break caught: returning success before applying the response-size limit to the whole body.
        var handler = new RecordingHandler(SuccessThenOversizedSoapResponse());
        var sender = CreateSender(handler);

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(false, "invalid-response"), result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_propagates_caller_requested_cancellation()
    {
        // Break caught: misreporting an explicit caller cancellation as a provider timeout/failure.
        var handler = new RecordingHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SoapHttpResponse("true");
        });
        var sender = CreateSender(handler);
        using var cancellation = new CancellationTokenSource();
        var send = sender.SendAsync(new SmsMessage(Mobile, Message), cancellation.Token);
        await handler.RequestReceived.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Registered_sender_uses_named_client_with_validated_https_endpoint_and_timeout()
    {
        // Break caught: bypassing typed validated configuration, the named client, or the replaceable application port.
        using var keyRing = new TemporaryDirectory();
        var handler = new RecordingHandler(SoapResponse("true"));
        using var host = BuildHost(ValidConfiguration(keyRing.Path), handler);
        await host.StartAsync();
        using var scope = host.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISmsSender>();

        var result = await sender.SendAsync(new SmsMessage(Mobile, Message), CancellationToken.None);

        Assert.Equal(new SmsSendResult(true, null), result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Registered_named_client_applies_the_configured_timeout()
    {
        // Break caught: registering the named client without its bounded SMS timeout.
        using var keyRing = new TemporaryDirectory();
        var configuration = ValidConfiguration(keyRing.Path);
        configuration[$"{SmsOptions.SectionName}:{nameof(SmsOptions.Timeout)}"] = "00:00:07";
        using var host = BuildHost(configuration, new RecordingHandler(SoapResponse("true")));
        await host.StartAsync();
        var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(SmsOptions.HttpClientName);

        Assert.True(client.Timeout == TimeSpan.FromSeconds(7), "The named SMS client must apply the configured timeout.");
    }

    [Theory]
    [InlineData("Endpoint", "http://sms.test.example/CompanySms.asmx")]
    [InlineData("Endpoint", "not-an-address")]
    [InlineData("Timeout", "00:00:00")]
    [InlineData("Timeout", "00:10:01")]
    public async Task Invalid_sms_options_fail_startup_without_echoing_values(string propertyName, string invalidValue)
    {
        // Break caught: starting with a non-HTTPS/unbounded SMS configuration or exposing the supplied value.
        using var keyRing = new TemporaryDirectory();
        var configuration = ValidConfiguration(keyRing.Path);
        configuration[$"{SmsOptions.SectionName}:{propertyName}"] = invalidValue;
        using var host = BuildHost(configuration, new RecordingHandler(SoapResponse("true")));

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Equal([$"{nameof(SmsOptions)}.{propertyName}"], exception.Failures);
        Assert.DoesNotContain(invalidValue, exception.Message, StringComparison.Ordinal);
    }

    private static SoapSmsSender CreateSender(RecordingHandler handler, TimeSpan? timeout = null)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(Endpoint),
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
        };
        return new SoapSmsSender(client);
    }

    private static IHost BuildHost(Dictionary<string, string?> configuration, RecordingHandler handler)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddSingleton(handler);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddHttpClient(SmsOptions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<RecordingHandler>());
        return builder.Build();
    }

    private static Dictionary<string, string?> ValidConfiguration(string keyRingPath)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "not-used",
            InitialCatalog = "EosDashboard_IntegrationTests",
            IntegratedSecurity = true,
        }.ConnectionString;
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:EosDashboard"] = connectionString,
            [$"AuthSecurity:HashingKey"] = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
            [$"AuthSecurity:SigningKey"] = Convert.ToBase64String(Enumerable.Range(33, 32).Select(value => (byte)value).ToArray()),
            ["AuthSecurity:Issuer"] = "EosDashboards.Tests",
            ["AuthSecurity:Audience"] = "EosDashboards.Tests.Client",
            ["AuthSecurity:AccessTokenLifetime"] = "00:10:00",
            ["AuthSecurity:SessionLifetime"] = "08:00:00",
            ["AuthSecurity:KeyRingPath"] = keyRingPath,
            [$"{SmsOptions.SectionName}:Endpoint"] = Endpoint,
            [$"{SmsOptions.SectionName}:Timeout"] = "00:00:05",
        };
    }

    private static string SoapResponse(string result) =>
        $"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><SendSmsMessageResponse xmlns=\"http://tempuri.org/\"><SendSmsMessageResult>{result}</SendSmsMessageResult></SendSmsMessageResponse></soap:Body></soap:Envelope>";

    private static string OversizedSoapResponse() =>
        $"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><padding>{new string('x', 64 * 1024)}</padding><SendSmsMessageResponse xmlns=\"http://tempuri.org/\"><SendSmsMessageResult>true</SendSmsMessageResult></SendSmsMessageResponse></soap:Body></soap:Envelope>";

    private static string SuccessThenOversizedSoapResponse() =>
        $"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body><SendSmsMessageResponse xmlns=\"http://tempuri.org/\"><SendSmsMessageResult>true</SendSmsMessageResult></SendSmsMessageResponse><padding>{new string('x', 64 * 1024)}</padding></soap:Body></soap:Envelope>";

    private static HttpResponseMessage SoapHttpResponse(string result) =>
        new(HttpStatusCode.OK) { Content = new StringContent(SoapResponse(result), Encoding.UTF8, "text/xml") };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _response;

        public RecordingHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
            : this(() => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "text/xml") }))
        {
        }

        public RecordingHandler(Func<Task<HttpResponseMessage>> response)
            : this(_ => response())
        {
        }

        public RecordingHandler(Func<CancellationToken, Task<HttpResponseMessage>> response)
        {
            _response = response;
        }

        public List<RecordedRequest> Requests { get; } = [];

        public TaskCompletionSource RequestReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var document = XDocument.Parse(body);
            var operation = document.Descendants().SingleOrDefault(element => element.Name.LocalName == "SendSmsMessage");
            var elements = operation?.Elements().ToArray() ?? [];
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.TryGetValues("SOAPAction", out var values) ? Assert.Single(values) : null,
                request.Content?.Headers.ContentType?.ToString(),
                elements.Select(element => element.Name.LocalName).SequenceEqual(["message", "mobile"]),
                elements.Length == 2 && elements[0].Value == Message && elements[1].Value == Mobile));
            RequestReceived.TrySetResult();
            return await _response(cancellationToken);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        string? SoapAction,
        string? ContentType,
        bool HasOrderedMessageAndMobile,
        bool SensitiveValuesRoundTrip)
    {
        public override string ToString() => nameof(RecordedRequest);
    }

    private static bool HasOnlySafeResultMetadata(SmsSendResult result) =>
        result.SafeErrorCode is null or "provider-rejected" or "provider-unavailable" or "invalid-response" or "timeout";

    private sealed class SlowBodyContent : HttpContent
    {
        public SlowBodyContent()
        {
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Xml);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new SlowReadStream());
    }

    private sealed class SlowReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private static readonly string TestRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EosDashboards.Tests"));
        private readonly string _path;

        public TemporaryDirectory()
        {
            _path = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestRoot, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(_path);
        }

        public string Path => _path;

        public void Dispose()
        {
            var resolvedPath = System.IO.Path.GetFullPath(_path);
            var expectedPrefix = TestRoot + System.IO.Path.DirectorySeparatorChar;
            if (!resolvedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Directory.GetParent(resolvedPath)?.FullName, TestRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Temporary path is outside the test root.");
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
            }
        }
    }
}
