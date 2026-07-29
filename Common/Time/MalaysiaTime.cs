namespace tms_template_net8.Common.Time;

/// <summary>
/// Malaysia local time (UTC+8, Asia/Kuala_Lumpur). No daylight saving.
/// Use <see cref="ForStorage"/> / <see cref="ForStorageString"/> before writing timestamps to the database.
/// </summary>
public static class MalaysiaTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static TimeZoneInfo TimeZone => Zone;

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime Today => Now.Date;

    public static string NowString(string format = "yyyy-MM-dd HH:mm:ss") => Now.ToString(format);

    /// <summary>
    /// Normalize a timestamp for DB storage as Malaysia wall-clock time (Unspecified kind).
    /// Null/default becomes <see cref="Now"/>.
    /// </summary>
    public static DateTime ForStorage(DateTime? value)
    {
        if (value is null || value.Value == default)
            return Now;

        return ForStorage(value.Value);
    }

    /// <summary>
    /// Convert any DateTime into Malaysia wall-clock time for DB storage.
    /// </summary>
    public static DateTime ForStorage(DateTime value)
    {
        var local = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, Zone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTimeFromUtc(value.ToUniversalTime(), Zone),
            _ => value // Unspecified: treat as already Malaysia local
        };

        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Normalize a string timestamp for DB storage. Empty/whitespace becomes <see cref="NowString"/>.
    /// </summary>
    public static string ForStorageString(string? value, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (string.IsNullOrWhiteSpace(value))
            return NowString(format);

        if (DateTime.TryParse(value.Trim(), out var parsed))
            return ForStorage(parsed).ToString(format);

        return value.Trim();
    }

    private static TimeZoneInfo ResolveZone()
    {
        // Windows: "Singapore Standard Time" (same offset as Malaysia).
        // Linux/macOS: "Asia/Kuala_Lumpur".
        foreach (var id in new[] { "Asia/Kuala_Lumpur", "Singapore Standard Time", "Malay Peninsula Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Malaysia Standard Time",
            TimeSpan.FromHours(8),
            "Malaysia Standard Time",
            "Malaysia Standard Time");
    }
}
