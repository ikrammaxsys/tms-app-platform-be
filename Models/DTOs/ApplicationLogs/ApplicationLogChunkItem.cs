namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogChunkItem
{
    public int Id { get; set; }
    public int ApplicationLogId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RemoteName { get; set; } = string.Empty;
}
