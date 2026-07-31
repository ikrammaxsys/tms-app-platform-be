namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogItem
{
    public int Id { get; set; }
    public string ApplicationId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string RemoteBasePath { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
}
