using System.Text;
using System.Text.Json;

namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public sealed class JsonBodyFormatter : IBodyFormatter
{
    public bool CanHandle(string? contentType, byte[] body)
    {
        if (IsJsonContentType(contentType))
            return true;

        var text = Encoding.UTF8.GetString(body);
        return LooksLikeJson(text);
    }

    public string Format(byte[] body, string? contentType)
    {
        var text = Encoding.UTF8.GetString(body);
        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (!LooksLikeJson(text) && !IsJsonContentType(contentType))
            return text;

        try
        {
            using var document = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return text;
        }
    }

    private static bool LooksLikeJson(string body)
    {
        return body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("[");
    }

    private static bool IsJsonContentType(string? contentType)
    {
        return contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
