namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class HostMetricsTimelineResponse
{
    public int ServerId { get; set; }
    public bool IsOnline { get; set; }
    public string CurrentStatus { get; set; } = "NoData";
    public DateTime? LastChecked { get; set; }
    public decimal? CurrentCpuUsage { get; set; }
    public HostResourceMetrics? CurrentRam { get; set; }
    public HostResourceMetrics? CurrentDisk { get; set; }
    public int Days { get; set; }
    public string Granularity { get; set; } = "day";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double UptimePercent { get; set; }
    public int TotalChecks { get; set; }
    public int UpCount { get; set; }
    public int DegradedCount { get; set; }
    public int DownCount { get; set; }
    public IReadOnlyList<HostMetricsTimelinePoint> Points { get; set; } = [];
}

public sealed class HostMetricsTimelinePoint
{
    public string Label { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double? UptimePercent { get; set; }
    public string Status { get; set; } = "NoData";
    public int TotalChecks { get; set; }
    public int UpCount { get; set; }
    public int DegradedCount { get; set; }
    public int DownCount { get; set; }
    public decimal? AvgCpuUsage { get; set; }
    public HostResourceMetrics? Ram { get; set; }
    public HostResourceMetrics? Disk { get; set; }
}

public sealed class HostResourceMetrics
{
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsagePercent { get; set; }
}
