using System.Text;
using OpenTelemetry.Instrumentation.Http;

namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public static class HttpClientBodyEnrichmentExtensions
{
    public static readonly IReadOnlyList<IBodyFormatter> DefaultFormatters =
    [
        new ProtobufCodedInputStreamBodyFormatter(),
        new JsonBodyFormatter(),
        new PlainTextBodyFormatter(),
    ];

    public static void ConfigureHttpClientBodyEnrichment(
        this HttpClientTraceInstrumentationOptions options,
        IEnumerable<IBodyFormatter>? formatters = null
    )
    {
        var formatterList = formatters ?? DefaultFormatters;

        options.EnrichWithHttpRequestMessage = (activity, request) =>
        {
            if (activity is null || request?.Content is null)
                return;

            var body = ReadRequestContent(request, formatterList, out var contentType);
            if (body is null)
                return;

            activity.SetTag("http.request.body", body);
        };

        options.EnrichWithHttpResponseMessage = (activity, response) =>
        {
            if (activity is null || response?.Content is null)
                return;

            var body = ReadResponseContent(response, formatterList, out var contentType);
            if (body is null)
                return;

            activity.SetTag("http.response.body", body);
        };
    }

    private static string? ReadRequestContent(
        HttpRequestMessage request,
        IEnumerable<IBodyFormatter> formatters,
        out string? contentType
    )
    {
        contentType = request.Content?.Headers.ContentType?.ToString();
        if (request.Content is null)
            return null;

        return ReadContentAsString(request.Content, contentType, formatters);
    }

    private static string? ReadResponseContent(
        HttpResponseMessage response,
        IEnumerable<IBodyFormatter> formatters,
        out string? contentType
    )
    {
        contentType = response.Content?.Headers.ContentType?.ToString();
        if (response.Content is null)
            return null;

        return ReadContentAsString(response.Content, contentType, formatters);
    }

    private static string? ReadContentAsString(
        HttpContent content,
        string? contentType,
        IEnumerable<IBodyFormatter>? formatters = null
    )
    {
        try
        {
            content.LoadIntoBufferAsync().GetAwaiter().GetResult();
            var bytes = content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            if (bytes is null || bytes.Length == 0)
                return string.Empty;

            var formatterList = formatters ?? DefaultFormatters;
            foreach (var formatter in formatterList)
            {
                if (formatter.CanHandle(contentType, bytes))
                    return formatter.Format(bytes, contentType);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
