using System.Text;

using Google.Protobuf;

namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public sealed class ProtobufCodedInputStreamBodyFormatter : IBodyFormatter
{
    public bool CanHandle(string? contentType, byte[] body)
    {
        return IsProtobufContentType(contentType);
    }

    public string Format(byte[] body, string? contentType)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Protobuf Message - CodedInputStream]");
            sb.Append(FormatCodedInputStream(body, indentLevel: 0));
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return $"[Protobuf binary data, {body.Length} bytes]";
        }
    }

    private static string FormatCodedInputStream(byte[] data, int indentLevel)
    {
        var sb = new StringBuilder();
        var cis = new CodedInputStream(data);
        var indent = new string(' ', (indentLevel + 1) * 2);

        while (!cis.IsAtEnd)
        {
            uint tag = cis.ReadTag();
            if (tag == 0)
                break;

            int fieldNumber = (int)(tag >> 3);
            var wireType = (WireFormat.WireType)(tag & 7);

            sb.Append($"{indent}{fieldNumber}: ({SimplifyWireType(wireType)}): ");

            switch (wireType)
            {
                case WireFormat.WireType.Varint:
                    sb.AppendLine(cis.ReadUInt64().ToString());
                    break;
                case WireFormat.WireType.Fixed32:
                    sb.AppendLine(cis.ReadFixed32().ToString());
                    break;
                case WireFormat.WireType.Fixed64:
                    sb.AppendLine(cis.ReadFixed64().ToString());
                    break;
                case WireFormat.WireType.LengthDelimited:
                    var bytes = cis.ReadBytes().ToByteArray();
                    if (TryFormatNestedMessage(bytes, indentLevel, out var nested))
                    {
                        sb.AppendLine();
                        sb.Append(nested);
                    }
                    else if (TryDecodeAsString(bytes, out var str))
                    {
                        sb.AppendLine($"\"{str}\"");
                    }
                    else
                    {
                        sb.AppendLine($"[bytes: {BitConverter.ToString(bytes)}]");
                    }
                    break;
                default:
                    sb.AppendLine("unsupported");
                    cis.SkipLastField();
                    break;
            }
        }

        return sb.ToString();
    }

    private static string SimplifyWireType(WireFormat.WireType wt) =>
        wt switch
        {
            WireFormat.WireType.Varint => "var",
            WireFormat.WireType.Fixed32 => "fx32",
            WireFormat.WireType.Fixed64 => "fx64",
            WireFormat.WireType.LengthDelimited => "len",
            _ => "unknown",
        };

    private static bool TryFormatNestedMessage(byte[] bytes, int indentLevel, out string formatted)
    {
        formatted = string.Empty;
        if (bytes.Length == 0)
            return false;

        try
        {
            var nested = FormatCodedInputStream(bytes, indentLevel + 1);
            if (string.IsNullOrWhiteSpace(nested))
                return false;

            formatted = nested.TrimEnd();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodeAsString(byte[] bytes, out string result)
    {
        result = string.Empty;
        if (bytes.Length == 0)
            return true;

        try
        {
            var str = Encoding.UTF8.GetString(bytes);
            if (IsLikelyString(str))
            {
                result = str;
                return true;
            }
        }
        catch
        {
            // Not valid UTF-8
        }
        return false;
    }

    private static bool IsLikelyString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true;

        // Check if all characters are printable or common whitespace
        foreach (var c in str)
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                return false;

        return true;
    }

    private static bool IsProtobufContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.Contains("protobuf", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("application/x-protobuf", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("application/grpc", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("proto", StringComparison.OrdinalIgnoreCase);
    }
}
