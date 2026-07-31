using System.Text.Json.Serialization;

namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class AgentHostReportRequest
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? HostId { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Hostname { get; set; }

    [JsonConverter(typeof(FlexibleIntJsonConverter))]
    public int? Status { get; set; }

    public bool? Available { get; set; }

    public double? CpuUsagePercent { get; set; }

    [JsonConverter(typeof(FlexibleIntJsonConverter))]
    public int? CpuCores { get; set; }

    public double? MemoryUsagePercent { get; set; }

    public long? MemoryTotalBytes { get; set; }

    public long? MemoryUsedBytes { get; set; }

    public long? MemoryFreeBytes { get; set; }

    public long? UptimeSeconds { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Platform { get; set; }

    public List<AgentHostDiskReport>? Disks { get; set; }

    public List<string>? Issues { get; set; }

    public DateTime? CollectedAt { get; set; }
}

public sealed class AgentHostDiskReport
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Mount { get; set; }

    public long? TotalBytes { get; set; }

    public long? FreeBytes { get; set; }

    public long? UsedBytes { get; set; }

    public double? UsagePercent { get; set; }
}
