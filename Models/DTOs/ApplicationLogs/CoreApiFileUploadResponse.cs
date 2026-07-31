namespace tms_template_net8.Models.DTOs.ApplicationLogs;

public sealed class CoreApiFileUploadData
{
    public string? FileName { get; set; }
    public string? Path { get; set; }
    public string? Extension { get; set; }
    public string? Url { get; set; }
}

public sealed class CoreApiFileUploadResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CoreApiFileUploadData? Data { get; set; }
}

public sealed class CoreApiFileUploadResult
{
    public string RemoteName { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
}
