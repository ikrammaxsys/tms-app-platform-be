namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogListResponse
{
    public int ApplicationId { get; set; }
    public string AppUid { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public IReadOnlyList<ApplicationLogDateItem> Dates { get; set; } = [];
}
