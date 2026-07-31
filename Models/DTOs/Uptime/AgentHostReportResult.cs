namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class AgentHostReportResult
{
    public int ServerId { get; set; }
    public string HostId { get; set; } = string.Empty;
    public int ServerMetricsId { get; set; }
    public DateTime Timestamp { get; set; }
}
