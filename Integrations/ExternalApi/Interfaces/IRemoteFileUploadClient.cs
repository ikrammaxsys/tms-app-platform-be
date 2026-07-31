namespace tms_template_net8.Integrations.ExternalApi.Interfaces;

using tms_template_net8.Models.DTOs.ApplicationLogs;

public interface IRemoteFileUploadClient
{
    Task<CoreApiFileUploadResult> UploadAsync(
        byte[] fileContent,
        string fileName,
        string directoryPath,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAsync(string filePath, CancellationToken cancellationToken = default);
}
