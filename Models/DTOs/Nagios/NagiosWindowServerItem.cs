namespace tms_template_net8.Models.DTOs.Nagios;

public sealed class NagiosWindowServerItem
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? CpuCount { get; set; }
    public string? CpuUsage { get; set; }
    public string? TotalMemory { get; set; }
    public string? UsedMemory { get; set; }
    public string? TotalDiskC { get; set; }
    public string? UsedDiskC { get; set; }
    public string? TotalDiskD { get; set; }
    public string? UsedDiskD { get; set; }
    public string? Uptime { get; set; }
    public string? Timestamp { get; set; }
}
