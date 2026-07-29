using System.Text.Json.Serialization;

namespace tms_template_net8.Models.DTOs.Uptime;

public sealed class AgentUptimeReportRequest
{
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? AppId { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Version { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Commit { get; set; }

    /// <summary>Agent health flag: 1 = Up, 0 = Down.</summary>
    [JsonConverter(typeof(FlexibleIntJsonConverter))]
    public int? Status { get; set; }
}
