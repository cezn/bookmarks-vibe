using System.Text;

namespace ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

public sealed class ProtobufBodyFormatter : IBodyFormatter
{
    public bool CanHandle(string? contentType, byte[] body) => IsProtobufContentType(contentType);

    public string Format(byte[] body, string? contentType) => FormatProtobuf(body);

    private static string FormatProtobuf(byte[] body)
    {
        try
        {
            var lines = ParseProtobufWireFormat(body, indentLevel: 0);
            var sb = new StringBuilder();
            sb.AppendLine("[Protobuf Message]");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
        catch
        {
            return $"[Protobuf binary data, {body.Length} bytes]";
        }
    }

    private static List<string> ParseProtobufWireFormat(byte[] data, int indentLevel)
    {
        var fields = new List<string>();
        var position = 0;
        var indent = new string(' ', (indentLevel + 1) * 2);

        while (position < data.Length)
        {
            if (!TryReadVarint(data, ref position, out var tag))
                break;

            var fieldNumber = (int)(tag >> 3);
            var wireType = (int)(tag & 0x7);

            string fieldDesc;
            switch (wireType)
            {
                case 0:
                    fieldDesc = TryReadVarint(data, ref position, out var value)
                        ? $"Field {fieldNumber} (varint): {value}"
                        : $"Field {fieldNumber} (varint): <error>";
                    break;
                case 1:
                    if (position + 8 <= data.Length)
                    {
                        fieldDesc = $"Field {fieldNumber} (fixed64): {BitConverter.ToUInt64(data, position)}";
                        position += 8;
                    }
                    else
                    {
                        fieldDesc = $"Field {fieldNumber} (fixed64): <error>";
                    }
                    break;
                case 2:
                    if (TryReadLengthDelimited(data, ref position, out var bytes))
                    {
                        var formatted = FormatLengthDelimited(bytes, indentLevel);
                        if (formatted.StartsWith("\n"))
                        {
                            fieldDesc = $"Field {fieldNumber} (nested message):{formatted}";
                        }
                        else
                        {
                            fieldDesc = $"Field {fieldNumber} (length-delimited): {formatted}";
                        }
                    }
                    else
                    {
                        fieldDesc = $"Field {fieldNumber} (length-delimited): <error>";
                    }
                    break;
                case 5:
                    if (position + 4 <= data.Length)
                    {
                        fieldDesc = $"Field {fieldNumber} (fixed32): {BitConverter.ToUInt32(data, position)}";
                        position += 4;
                    }
                    else
                    {
                        fieldDesc = $"Field {fieldNumber} (fixed32): <error>";
                    }
                    break;
                default:
                    fieldDesc = $"Field {fieldNumber} (unknown wire type {wireType})";
                    break;
            }

            fields.Add(indent + fieldDesc);
        }

        return fields;
    }

    private static bool TryReadVarint(byte[] data, ref int position, out ulong value)
    {
        value = 0;
        var shift = 0;

        while (position < data.Length)
        {
            var b = data[position++];
            value |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return true;

            shift += 7;
            if (shift >= 64)
                return false;
        }

        return false;
    }

    private static bool TryReadLengthDelimited(byte[] data, ref int position, out byte[] value)
    {
        value = Array.Empty<byte>();

        if (!TryReadVarint(data, ref position, out var length))
            return false;

        if (position + (int)length > data.Length)
            return false;

        value = new byte[length];
        Array.Copy(data, position, value, 0, (int)length);
        position += (int)length;
        return true;
    }

    private static string FormatLengthDelimited(byte[] bytes, int indentLevel)
    {
        // Try to parse as UTF-8 string
        try
        {
            var str = Encoding.UTF8.GetString(bytes);
            if (IsLikelyString(str))
                return $"\"{str}\"";
        }
        catch { }

        // Try to parse as nested message
        if (bytes.Length > 0 && TryParseAsNestedMessage(bytes, indentLevel, out var nestedResult))
            return nestedResult;

        return $"[bytes, {bytes.Length} length]";
    }

    private static bool TryParseAsNestedMessage(byte[] bytes, int indentLevel, out string result)
    {
        result = string.Empty;

        // Basic heuristic: check if it starts with a valid tag
        if (bytes.Length == 0 || (bytes[0] & 0x07) > 5)
            return false;

        try
        {
            var nestedFields = ParseProtobufWireFormat(bytes, indentLevel + 1);
            if (nestedFields.Count == 0)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var line in nestedFields)
            {
                sb.AppendLine(line);
            }
            result = sb.ToString().TrimEnd();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLikelyString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true;

        // Check if all characters are printable or common whitespace
        foreach (var c in str)
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                return false;
        }

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
