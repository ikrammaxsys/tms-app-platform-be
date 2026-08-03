namespace tms_template_net8.Models.DTOs.Agent;

public sealed class AgentItem
{
    public int Id { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ServerId { get; set; }
    public string AuthToken { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? LastReadyAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ConfigJson { get; set; }

    // Joined display fields
    public string ServerDomain { get; set; } = string.Empty;
    public string ServerEnvironment { get; set; } = string.Empty;
}
