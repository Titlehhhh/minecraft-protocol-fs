using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Converters;

public sealed class ProtocolRangeJsonConverter : JsonConverter<ProtocolRange>
{
    // ---------- VALUE ----------

    public override ProtocolRange Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("ProtocolRange must be a string");

        return Parse(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ProtocolRange value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    // ---------- DICTIONARY KEY ----------

    public override ProtocolRange ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void WriteAsPropertyName(
        Utf8JsonWriter writer,
        ProtocolRange value,
        JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    // ---------- SHARED PARSER ----------

    private static ProtocolRange Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new JsonException("ProtocolRange string is empty");

        if (!text.Contains('-'))
        {
            if (!int.TryParse(text, out var v))
                throw new JsonException($"Invalid ProtocolRange: {text}");

            return new ProtocolRange(v, v);
        }

        var parts = text.Split('-', 2);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var from)
            || !int.TryParse(parts[1], out var to))
        {
            throw new JsonException($"Invalid ProtocolRange: {text}");
        }

        return new ProtocolRange(from, to);
    }
}