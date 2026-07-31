using System.Text.Json;

namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class ApplicationLogChunkContentResponse
{
    public int ChunkId { get; set; }
    public string ChunkName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public bool HasNext { get; set; }
    public string? NextChunk { get; set; }
    public JsonElement LogJson { get; set; }
}
