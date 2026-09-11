namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public interface IBodyFormatter
{
    bool CanHandle(string? contentType, byte[] body);
    string Format(byte[] body, string? contentType);
}
