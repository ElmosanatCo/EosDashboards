using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;

namespace EosDashboards.Infrastructure.Sms;

public sealed class SoapSmsSender(HttpClient httpClient) : ISmsSender
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string OperationNamespace = "http://tempuri.org/";
    private const string SoapAction = "http://tempuri.org/SendSmsMessage";
    private const string ResultElementName = "SendSmsMessageResult";
    private const int MaximumResponseCharacters = 64 * 1024;

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (httpClient.Timeout != Timeout.InfiniteTimeSpan)
        {
            operationCancellation.CancelAfter(httpClient.Timeout);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = CreateSoapContent(message),
            };
            request.Headers.Add("SOAPAction", SoapAction);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                operationCancellation.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new SmsSendResult(false, "provider-unavailable");
            }

            var responseBody = await ReadBoundedResponseAsync(response, operationCancellation.Token);
            return responseBody is null
                ? new SmsSendResult(false, "invalid-response")
                : ParseResponse(responseBody);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new SmsSendResult(false, "timeout");
        }
        catch (HttpRequestException)
        {
            return new SmsSendResult(false, "provider-unavailable");
        }
        catch (XmlException)
        {
            return new SmsSendResult(false, "invalid-response");
        }
        catch (IOException)
        {
            return new SmsSendResult(false, "invalid-response");
        }
    }

    private static HttpContent CreateSoapContent(SmsMessage message)
    {
        using var stream = new MemoryStream();
        var writerSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
        };
        using (var writer = XmlWriter.Create(stream, writerSettings))
        {
            writer.WriteStartElement("soap", "Envelope", SoapNamespace);
            writer.WriteStartElement("soap", "Body", SoapNamespace);
            writer.WriteStartElement("SendSmsMessage", OperationNamespace);
            writer.WriteElementString("message", message.Text);
            writer.WriteElementString("mobile", message.Mobile);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        var content = new ByteArrayContent(stream.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml")
        {
            CharSet = "utf-8",
        };
        return content;
    }

    private static async Task<byte[]?> ReadBoundedResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var responseBuffer = new MemoryStream();
        var readBuffer = new byte[8192];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
            {
                return responseBuffer.ToArray();
            }

            if (responseBuffer.Length > MaximumResponseCharacters - bytesRead)
            {
                return null;
            }

            await responseBuffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static SmsSendResult ParseResponse(byte[] responseBody)
    {
        using var stream = new MemoryStream(responseBody, writable: false);
        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumResponseCharacters,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
        };
        using var reader = XmlReader.Create(stream, readerSettings);
        string? resultValue = null;
        var resultCount = 0;

        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == ResultElementName)
            {
                resultCount++;
                var candidateValue = reader.ReadElementContentAsString();
                if (resultValue is null)
                {
                    resultValue = candidateValue;
                }

                continue;
            }

            reader.Read();
        }

        if (resultCount != 1 || !bool.TryParse(resultValue, out var succeeded))
        {
            return new SmsSendResult(false, "invalid-response");
        }

        return succeeded
            ? new SmsSendResult(true, null)
            : new SmsSendResult(false, "provider-rejected");
    }
}
