namespace tms_template_net8.Models.DTOs.Agent;

public sealed class AgentConfigResponse
{
    public string AgentUid { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
}
