using System.Globalization;
using System.Text.Json;

namespace tms_template_net8.Models.DTOs.ApplicationLogs;

internal static class ApplicationLogTimeRangeHelper
{
    public static DateTime ParseRequiredDateTime(string value, string fieldName)
    {
        var parsed = TryParseDateTime(value);
        if (parsed is null)
            throw new ArgumentException($"{fieldName} must be a valid ISO 8601 datetime.");

        return parsed.Value;
    }

    public static DateTime? TryParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (DateTime.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
            || DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }

    public static int CountLogEntries(string logJson)
    {
        using var document = JsonDocument.Parse(logJson);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => document.RootElement.GetArrayLength(),
            JsonValueKind.Object => 1,
            _ => 0
        };
    }
}
