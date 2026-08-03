namespace tms_template_net8.Models.DTOs.Agent;

public sealed class AgentUpsertRequest
{
    public string? Name { get; set; }
    public string? Uid { get; set; }
    public int? ServerId { get; set; }
    public string? AuthToken { get; set; }
}
