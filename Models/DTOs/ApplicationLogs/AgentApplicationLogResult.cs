namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class AgentApplicationLogResult
{
    public int ApplicationLogId { get; set; }
    public int ChunkId { get; set; }
    public string ChunkName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
}
