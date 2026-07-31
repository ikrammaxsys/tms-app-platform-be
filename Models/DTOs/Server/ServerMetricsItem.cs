namespace tms_template_net8.Models.DTOs.Server;

public sealed class ServerMetricsItem
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public int CpuCores { get; set; }
    public decimal CpuUsage { get; set; }
    public long RamTotal { get; set; }
    public long RamUsage { get; set; }
    public long RamAvailable { get; set; }
    public long DiskTotal { get; set; }
    public long DiskUsed { get; set; }
    public long DiskAvailable { get; set; }
    public DateTime Timestamp { get; set; }
}
