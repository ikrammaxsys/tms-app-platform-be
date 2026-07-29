namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class UptimeTimelineResponse
{
    public int ApplicationId { get; set; }
    public bool IsOnline { get; set; }
    public string CurrentStatus { get; set; } = "NoData";
    public DateTime? LastChecked { get; set; }
    public int Days { get; set; }
    public string Granularity { get; set; } = "day";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double UptimePercent { get; set; }
    public int TotalChecks { get; set; }
    public int UpCount { get; set; }
    public int DegradedCount { get; set; }
    public int DownCount { get; set; }
    public IReadOnlyList<UptimeTimelinePoint> Points { get; set; } = [];
}

public sealed class UptimeTimelinePoint
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
}
