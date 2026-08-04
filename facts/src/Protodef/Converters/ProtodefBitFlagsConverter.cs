using System.Text.Json;
using System.Text.Json.Serialization;

namespace Protodef.Converters;

public sealed class ProtodefBitFlagsConverter : JsonConverter<ProtodefBitFlags>
{
    public override ProtodefBitFlags? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject but got {reader.TokenType}");

        string? type = null;
        object[]? flags = null;
        bool big = false;
        int shift = 0;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName but got {reader.TokenType}");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "type":
                    type = reader.GetString();
                    if (type is null)
                        throw new JsonException("Property 'type' must be a string");
                    break;

                case "flags":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException($"Expected StartArray for flags but got {reader.TokenType}");
                    
                    var flagList = new List<object>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                            throw new JsonException($"Flag must be a string, but got {reader.TokenType}");
                        
                        flagList.Add(reader.GetString() ?? throw new JsonException("Flag string cannot be null"));
                    }
                    flags = flagList.ToArray();
                    break;

                case "big":
                    big = reader.GetBoolean();
                    break;

                case "shift":
                    shift = reader.GetInt32();
                    break;

                default:
                    // Skip unknown properties
                    reader.Skip();
                    break;
            }
        }

        if (type is null)
            throw new JsonException("Missing required property 'type' in ProtodefBitFlags");

        if (flags is null)
            throw new JsonException("Missing required property 'flags' in ProtodefBitFlags");

        return new ProtodefBitFlags(type, flags, big, shift);
    }

    public override void Write(Utf8JsonWriter writer, ProtodefBitFlags value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteStringValue(value.Type);
        writer.WritePropertyName("flags");
        writer.WriteStartArray();
        foreach (var flag in value.Flags)
        {
            if (flag is string str)
            {
                writer.WriteStringValue(str);
            }
            else
            {
                throw new JsonException($"Flag must be a string, but got {flag?.GetType().Name ?? "null"}");
            }
        }
        writer.WriteEndArray();
        
        if (value.Big)
        {
            writer.WritePropertyName("big");
            writer.WriteBooleanValue(value.Big);
        }
        
        if (value.Shift != 0)
        {
            writer.WritePropertyName("shift");
            writer.WriteNumberValue(value.Shift);
        }

        writer.WriteEndObject();
    }
}
