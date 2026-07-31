using System.Net.Http.Headers;
using System.Net.Http.Json;
using tms_template_net8.Integrations.ExternalApi.Interfaces;
using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Integrations.ExternalApi;

public sealed class RemoteFileUploadClient : IRemoteFileUploadClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemoteFileUploadClient> _logger;

    public RemoteFileUploadClient(HttpClient httpClient, ILogger<RemoteFileUploadClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CoreApiFileUploadResult> UploadAsync(
        byte[] fileContent,
        string fileName,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var folderPath = ApplicationLogPayloadHelper.NormalizeDirectoryPath(directoryPath);
        var intendedFilePath = ApplicationLogPayloadHelper.BuildChunkFilePath(folderPath, fileName);

        using var content = new MultipartFormDataContent();
        var fileContentPart = new ByteArrayContent(fileContent);
        fileContentPart.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
        content.Add(fileContentPart, "file", fileName);
        content.Add(new StringContent(folderPath), "path");
        content.Add(new StringContent(fileName), "fileName");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("/api/v1/files/upload", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Remote file upload request failed. Path={Path}", intendedFilePath);
            throw new InvalidOperationException(
                "Remote file upload request failed. Ensure CoreApi:BaseUrl uses https if the Core API runs on HTTPS.",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Remote file upload failed. Status={StatusCode}, Path={Path}, Response={Response}",
                    (int)response.StatusCode,
                    intendedFilePath,
                    body);
                throw new InvalidOperationException($"Remote file upload failed with status {(int)response.StatusCode}.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<CoreApiFileUploadResponse>(cancellationToken);
            if (envelope is null || !envelope.Success || envelope.Data is null)
                throw new InvalidOperationException("Remote file upload did not return file metadata.");

            if (string.IsNullOrWhiteSpace(envelope.Data.FileName) || string.IsNullOrWhiteSpace(envelope.Data.Path))
                throw new InvalidOperationException("Remote file upload response is missing fileName or path.");

            return new CoreApiFileUploadResult
            {
                RemoteName = envelope.Data.FileName,
                RemotePath = envelope.Data.Path
            };
        }
    }

    public async Task<byte[]> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v1/files/get?fileName={Uri.EscapeDataString(filePath)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Remote file get failed. Status={StatusCode}, Path={Path}, Response={Response}",
                (int)response.StatusCode,
                filePath,
                body);
            throw new InvalidOperationException($"Remote file get failed with status {(int)response.StatusCode}.");
        }

        var envelope = await response.Content.ReadFromJsonAsync<CoreApiFileGetResponse>(cancellationToken);
        if (envelope is null || !envelope.Success || string.IsNullOrWhiteSpace(envelope.Data?.Url))
            throw new InvalidOperationException("Remote file get did not return a download URL.");

        using var downloadClient = new HttpClient();
        var fileResponse = await downloadClient.GetAsync(envelope.Data.Url, cancellationToken);
        if (!fileResponse.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Remote file download failed. Status={StatusCode}, Path={Path}, Url={Url}",
                (int)fileResponse.StatusCode,
                filePath,
                envelope.Data.Url);
            throw new InvalidOperationException($"Remote file download failed with status {(int)fileResponse.StatusCode}.");
        }

        return await fileResponse.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
