using System.Text;

namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public sealed class PlainTextBodyFormatter : IBodyFormatter
{
    public bool CanHandle(string? contentType, byte[] body) => true;

    public string Format(byte[] body, string? contentType) => Encoding.UTF8.GetString(body);
}
