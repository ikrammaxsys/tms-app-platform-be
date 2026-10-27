namespace tms_template_net8.Models.DTOs.Nagios;

/// <summary>One Nagios service-status row (HOST / SERVICE / REMARKS / UPDATED_DATE).</summary>
public sealed class NagiosServiceStatusRow
{
    public string Host { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
}
