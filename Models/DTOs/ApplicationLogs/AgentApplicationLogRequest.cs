using System.Text.Json.Serialization;
using tms_template_net8.Models.DTOs.Uptime;

namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class AgentApplicationLogRequest
{
    [JsonPropertyName("appUid")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? AppUid { get; set; }

    [JsonPropertyName("date")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Date { get; set; }

    [JsonPropertyName("log_json")]
    [JsonConverter(typeof(FlexibleLogJsonStringConverter))]
    public string? LogJson { get; set; }

    [JsonPropertyName("time_start")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? TimeStart { get; set; }

    [JsonPropertyName("time_end")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? TimeEnd { get; set; }
}
