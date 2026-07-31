using System.Globalization;

namespace tms_template_net8.Models.DTOs.ApplicationLogs;

internal static class ApplicationLogPayloadHelper
{
    private static readonly string[] SupportedDateFormats =
    [
        "yyyy-MM-dd",
        "d-M-yyyy",
        "dd-MM-yyyy",
        "d/M/yyyy",
        "dd/MM/yyyy",
        "M/d/yyyy",
        "MM/dd/yyyy"
    ];

    public static string NormalizeLogJson(string logJson)
    {
        var trimmed = logJson.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("log_json is required.");

        return trimmed;
    }

    public static string ExtractDateOnly(string date)
    {
        var trimmed = date.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex > 0)
            trimmed = trimmed[..spaceIndex];

        trimmed = trimmed.Replace('\\', '/');

        if (DateTime.TryParseExact(
                trimmed,
                SupportedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed)
            || DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return trimmed.Replace('/', '-');
    }

    public static string ToPathSegment(string date) => ExtractDateOnly(date);

    public static string NormalizeDirectoryPath(string directoryPath)
    {
        var normalized = directoryPath.Trim().Replace('\\', '/').TrimEnd('/');
        return normalized.StartsWith('/') ? normalized + "/" : $"/{normalized}/";
    }

    public static string BuildChunkFilePath(string directoryPath, string fileName)
    {
        var directory = NormalizeDirectoryPath(directoryPath).TrimEnd('/');
        return $"{directory}/{fileName}";
    }

    public static string BuildStoredChunkPath(string logDate, string remoteName)
    {
        var date = ExtractDateOnly(logDate).Trim('/');
        var fileName = remoteName.Trim().Replace('\\', '/').TrimStart('/');
        return $"{date}/{fileName}";
    }

    public static string BuildRemoteDownloadPath(string storedPath, string? storagePrefix)
    {
        var normalized = storedPath.Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Stored chunk path is required.");

        var prefix = storagePrefix?.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(prefix))
            return normalized;

        if (normalized.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return $"{prefix}/{normalized}";
    }

    public static bool IsValidLogJson(string? logJson) => !string.IsNullOrWhiteSpace(logJson);

    public static bool ChunkMatches(ApplicationLogChunkItem chunk, string identifier)
    {
        var normalized = identifier.Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var chunkPath = chunk.Path.Trim().Replace('\\', '/').TrimStart('/');
        var remoteName = chunk.RemoteName.Trim().Replace('\\', '/').TrimStart('/');

        if (string.Equals(chunk.Name, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(chunkPath, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(remoteName, normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(remoteName, Path.GetFileName(normalized), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return chunkPath.EndsWith($"/{normalized}", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith($"/{remoteName}", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(remoteName, StringComparison.OrdinalIgnoreCase);
    }
}
