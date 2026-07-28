namespace tms_template_net8.Models.DTOs.Application;
public sealed class ApplicationUptimeLogItem
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int? Latency { get; set; }
    public string Status { get; set; } = "Up";
    public DateTime Timestamp { get; set; }
}
