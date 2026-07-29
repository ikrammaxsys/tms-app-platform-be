using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tms_template_net8.Models.DTOs.Uptime;

/// <summary>
/// Accepts JSON number or numeric string (e.g. status: 1 or "1").
/// </summary>
public sealed class FlexibleIntJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var i))
                    return i;
                if (reader.TryGetInt64(out var l))
                    return (int)l;
                return (int)reader.GetDouble();
            case JsonTokenType.String:
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
                throw new JsonException($"Unable to convert \"{s}\" to int.");
            case JsonTokenType.True:
                return 1;
            case JsonTokenType.False:
                return 0;
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing int.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value);
    }
}
