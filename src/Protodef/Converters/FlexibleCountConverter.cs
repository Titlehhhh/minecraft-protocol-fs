using System.Text.Json;
using System.Text.Json.Serialization;

namespace Protodef.Converters;

public class FlexibleCountConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unsupported token type {reader.TokenType} for Count")
        };
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case int i:
                writer.WriteNumberValue(i);
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException($"Unsupported type {value?.GetType()} for Count");
        }
    }
}