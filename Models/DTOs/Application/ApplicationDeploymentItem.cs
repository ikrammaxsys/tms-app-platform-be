namespace tms_template_net8.Models.DTOs.Application;

public sealed class ApplicationDeploymentItem
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string CommitNo { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    // Joined display fields
    public string ApplicationName { get; set; } = string.Empty;
}
