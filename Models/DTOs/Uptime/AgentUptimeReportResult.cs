namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class AgentUptimeReportResult
{
    public int ApplicationId { get; set; }
    public string ApplicationUid { get; set; } = string.Empty;
    public bool VersionDrift { get; set; }
    public string PreviousVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public int UptimeLogId { get; set; }
    public int? DeploymentId { get; set; }
}
