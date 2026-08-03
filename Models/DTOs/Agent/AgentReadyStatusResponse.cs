namespace tms_template_net8.Models.DTOs.Agent;

public sealed class AgentReadyStatusResponse
{
    public string AgentUid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastReadyAt { get; set; }
    public string ServerDomain { get; set; } = string.Empty;
}
