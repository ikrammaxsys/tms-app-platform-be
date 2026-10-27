using System.Globalization;
using System.Text.RegularExpressions;
using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Common.Nagios;

/// <summary>
/// Parses Nagios SERVICE/REMARKS text into linux / windows snapshot rows.
/// </summary>
public static partial class NagiosRemarksParser
{
    private const int Varchar100 = 100;

    public static IReadOnlyList<NagiosLinuxServerItem> BuildLinuxSnapshots(
        IEnumerable<NagiosServiceStatusRow> rows)
    {
        var result = new List<NagiosLinuxServerItem>();

        foreach (var group in GroupByHost(rows))
        {
            var services = ToServiceMap(group);
            result.Add(MapLinux(group.Key, services, LatestUpdatedDate(group)));
        }

        return result;
    }

    public static IReadOnlyList<NagiosWindowServerItem> BuildWindowsSnapshots(
        IEnumerable<NagiosServiceStatusRow> rows)
    {
        var result = new List<NagiosWindowServerItem>();

        foreach (var group in GroupByHost(rows))
        {
            var services = ToServiceMap(group);
            result.Add(MapWindows(group.Key, services, LatestUpdatedDate(group)));
        }

        return result;
    }

    private static IEnumerable<IGrouping<string, NagiosServiceStatusRow>> GroupByHost(
        IEnumerable<NagiosServiceStatusRow> rows) =>
        rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Host))
            .GroupBy(r => r.Host.Trim(), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> ToServiceMap(
        IGrouping<string, NagiosServiceStatusRow> group) =>
        group
            .GroupBy(r => r.Service?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Remarks ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    private static DateTime? LatestUpdatedDate(IGrouping<string, NagiosServiceStatusRow> group)
    {
        var dates = group
            .Where(r => r.UpdatedDate.HasValue)
            .Select(r => r.UpdatedDate!.Value)
            .ToList();

    Console.WriteLine($"LatestUpdatedDate: {dates.Count} dates");
            foreach (var date in dates)
            {
                Console.WriteLine($"Date: {date:yyyy-MM-dd HH:mm:ss.fff}");
            }
            Console.WriteLine($"LatestUpdatedDate: {dates}");
        return dates.Count > 0 ? dates.Max() : null;
    }

    private static NagiosLinuxServerItem MapLinux(
        string host,
        IReadOnlyDictionary<string, string> services,
        DateTime? updatedDate)
    {
        services.TryGetValue("Current Load", out var loadRemarks);
        services.TryGetValue("Swap Usage", out var swapRemarks);
        services.TryGetValue("Root Partition", out var diskRemarks);
        services.TryGetValue("PING", out var pingRemarks);
        services.TryGetValue("Total Processes", out var procsRemarks);

        return new NagiosLinuxServerItem
        {
            Name = Clip(host, Varchar100),
            CurrentLoad = Clip(ParseLoadAverage(loadRemarks), Varchar100),
            Swap = Clip(ParseSwap(swapRemarks), Varchar100),
            DiskUsage = Clip(ParseLinuxDisk(diskRemarks), Varchar100),
            PingMs = Clip(ParsePingMs(pingRemarks), Varchar100),
            TotalProcess = Clip(ParseTotalProcesses(procsRemarks), Varchar100),
            Timestamp = updatedDate
        };
    }

    private static NagiosWindowServerItem MapWindows(
        string host,
        IReadOnlyDictionary<string, string> services,
        DateTime? updatedDate)
    {
        services.TryGetValue("CPU Count", out var cpuCountRemarks);
        services.TryGetValue("CPU Usage", out var cpuUsageRemarks);
        services.TryGetValue("Memory Usage", out var memoryRemarks);
        services.TryGetValue("C Drive usage", out var diskCRemarks);
        services.TryGetValue("D Drive usage", out var diskDRemarks);
        services.TryGetValue("Uptime", out var uptimeRemarks);

        var (totalMem, usedMem) = ParseMemory(memoryRemarks);
        var (totalC, usedC) = ParseDriveUsage(diskCRemarks);
        var (totalD, usedD) = ParseDriveUsage(diskDRemarks);

        return new NagiosWindowServerItem
        {
            Name = Clip(host, Varchar100),
            CpuCount = Clip(ParseCpuCount(cpuCountRemarks), Varchar100),
            CpuUsage = Clip(ParseCpuUsage(cpuUsageRemarks), Varchar100),
            TotalMemory = Clip(totalMem, Varchar100),
            UsedMemory = Clip(usedMem, Varchar100),
            TotalDiskC = Clip(totalC, Varchar100),
            UsedDiskC = Clip(usedC, Varchar100),
            TotalDiskD = Clip(totalD, Varchar100),
            UsedDiskD = Clip(usedD, Varchar100),
            Uptime = ParseUptime(uptimeRemarks),
            Timestamp = updatedDate
        };
    }

    // --- Linux parsers ---

    // "OK - load average: 0.58, 0.42, 0.42"
    internal static string? ParseLoadAverage(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = LoadAverageRegex().Match(remarks);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    // "PING OK - Packet loss = 0%, RTA = 18.96 ms"
    internal static string? ParsePingMs(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = PingRtaRegex().Match(remarks);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    // "DISK OK - free space: / 59790 MiB (83.45% inode=...)"
    internal static string? ParseLinuxDisk(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = LinuxDiskFreeRegex().Match(remarks);
        if (!m.Success) return null;
        var free = $"{m.Groups[1].Value} {m.Groups[2].Value}".Trim();
        if (m.Groups[3].Success)
            return $"{free} free ({m.Groups[3].Value}%)";
        return $"{free} free";
    }

    // "SWAP OK - 41% free (1645 MB out of 4051 MB)"
    internal static string? ParseSwap(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = SwapRegex().Match(remarks);
        if (!m.Success) return null;
        // used = total - free
        if (long.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var freeMb)
            && long.TryParse(m.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var totalMb))
        {
            var usedMb = Math.Max(0, totalMb - freeMb);
            return $"{usedMb} MB / {totalMb} MB ({m.Groups[1].Value}% free)";
        }

        return $"{m.Groups[2].Value} MB out of {m.Groups[3].Value} MB";
    }

    // "PROCS OK: 180 processes with STATE = RSZDT"
    internal static string? ParseTotalProcesses(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = ProcsRegex().Match(remarks);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    // --- Windows parsers ---

    // "OK: Count was 2 cores"
    internal static string? ParseCpuCount(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = CpuCountRegex().Match(remarks);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    // "OK: Percent was 60.60 %"
    internal static string? ParseCpuUsage(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = CpuUsageRegex().Match(remarks);
        return m.Success ? $"{m.Groups[1].Value} %" : null;
    }

    // "OK: Memory usage was 45.90 % (Available: 4.33 GiB, Total: 8.00 GiB, Free: 4.33 GiB, Used: 3.67 GiB)"
    internal static (string? Total, string? Used) ParseMemory(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return (null, null);
        var total = LabeledSizeRegex("Total").Match(remarks);
        var used = LabeledSizeRegex("Used").Match(remarks);
        return (
            total.Success ? $"{total.Groups[1].Value} {total.Groups[2].Value}" : null,
            used.Success ? $"{used.Groups[1].Value} {used.Groups[2].Value}" : null);
    }

    // "OK: Used disk space was 52.80 % (Used: 66.76 GiB, Free: 59.75 GiB, Total: 126.51 GiB)"
    internal static (string? Total, string? Used) ParseDriveUsage(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return (null, null);
        var total = LabeledSizeRegex("Total").Match(remarks);
        var used = LabeledSizeRegex("Used").Match(remarks);
        return (
            total.Success ? $"{total.Groups[1].Value} {total.Groups[2].Value}" : null,
            used.Success ? $"{used.Groups[1].Value} {used.Groups[2].Value}" : null);
    }

    // "OK: Uptime was 11 days 8 hours 55 minutes 47 seconds"
    internal static string? ParseUptime(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        var m = UptimeRegex().Match(remarks);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    private static Regex LabeledSizeRegex(string label) =>
        new($@"{Regex.Escape(label)}:\s*([\d.]+)\s*([A-Za-z]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [GeneratedRegex(@"load\s+average:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LoadAverageRegex();

    [GeneratedRegex(@"RTA\s*=\s*([\d.]+)\s*ms", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PingRtaRegex();

    [GeneratedRegex(@"free\s+space:\s*/\s*([\d.]+)\s*([A-Za-z]+)(?:[^(]*\((\d+(?:\.\d+)?)\s*%)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinuxDiskFreeRegex();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*%\s*free\s*\((\d+)\s*MB\s*out of\s*(\d+)\s*MB\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SwapRegex();

    [GeneratedRegex(@"PROCS\s+OK:\s*(\d+)\s*processes", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProcsRegex();

    [GeneratedRegex(@"Count\s+was\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CpuCountRegex();

    [GeneratedRegex(@"Percent\s+was\s*([\d.]+)\s*%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CpuUsageRegex();

    [GeneratedRegex(@"Uptime\s+was\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UptimeRegex();
}
