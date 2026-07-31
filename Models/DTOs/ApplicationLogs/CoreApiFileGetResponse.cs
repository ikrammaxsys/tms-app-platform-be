namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class CoreApiFileGetData
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
    public string? Message { get; set; }
}

public sealed class CoreApiFileGetResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CoreApiFileGetData? Data { get; set; }
}
