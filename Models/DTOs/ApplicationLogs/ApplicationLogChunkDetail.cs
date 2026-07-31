namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogChunkDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string RemoteName { get; set; } = string.Empty;
}
