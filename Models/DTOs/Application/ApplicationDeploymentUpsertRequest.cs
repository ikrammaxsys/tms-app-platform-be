namespace tms_template_net8.Models.DTOs.Application;

public sealed class ApplicationDeploymentUpsertRequest
{
    public int ApplicationId { get; set; }
    public string? CommitNo { get; set; }
    public string? Version { get; set; }
    public string? Timestamp { get; set; }
}
