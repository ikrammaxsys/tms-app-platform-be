namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogDateItem
{
    public int ApplicationLogId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string RemoteBasePath { get; set; } = string.Empty;
    public IReadOnlyList<ApplicationLogChunkDetail> Chunks { get; set; } = [];
}
