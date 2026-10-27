namespace tms_template_net8.Models.DTOs.Nagios;

public sealed class NagiosLinuxServerItem
{
    public string? Name { get; set; }
    public string? CurrentLoad { get; set; }
    public string? Swap { get; set; }
    public string? DiskUsage { get; set; }
    public string? PingMs { get; set; }
    public string? TotalProcess { get; set; }
    public DateTime? Timestamp { get; set; }
}
