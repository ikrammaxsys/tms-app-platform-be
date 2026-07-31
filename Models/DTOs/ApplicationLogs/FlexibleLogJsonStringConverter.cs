using System.Text.Json;
using System.Text.Json.Serialization;

namespace tms_template_net8.Models.DTOs.ApplicationLogs;

/// <summary>
/// Accepts log_json as a JSON string from the agent, or as a raw JSON object/array.
/// Always resolves to the inner JSON text for storage.
/// </summary>
public sealed class FlexibleLogJsonStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartObject or JsonTokenType.StartArray =>
                JsonDocument.ParseValue(ref reader).RootElement.GetRawText(),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for log_json.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
