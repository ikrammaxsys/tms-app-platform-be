using System.IO.Compression;
using System.Text;
using System.Text.Json;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Integrations.ExternalApi.Interfaces;
using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Services;

public sealed class ApplicationLogService : IApplicationLogService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationLogRepository _applicationLogs;
    private readonly IApplicationLogChunkRepository _applicationLogChunks;
    private readonly IRemoteFileUploadClient _remoteFileUploadClient;
    private readonly string? _storagePrefix;

    public ApplicationLogService(
        IApplicationRepository applicationRepository,
        IApplicationLogRepository applicationLogs,
        IApplicationLogChunkRepository applicationLogChunks,
        IRemoteFileUploadClient remoteFileUploadClient,
        IConfiguration configuration)
    {
        _applicationRepository = applicationRepository;
        _applicationLogs = applicationLogs;
        _applicationLogChunks = applicationLogChunks;
        _remoteFileUploadClient = remoteFileUploadClient;
        _storagePrefix = configuration["CoreApi:StoragePrefix"];
    }

    public async Task<AgentApplicationLogResult?> IngestAsync(
        AgentApplicationLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var appUid = (request.AppUid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(appUid))
            return null;

        var application = await _applicationRepository.GetByUidAsync(appUid, cancellationToken);
        if (application is null)
            return null;

        var date = (request.Date ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("date is required.");

        if (!ApplicationLogPayloadHelper.IsValidLogJson(request.LogJson))
            throw new ArgumentException("log_json is required.");

        var jsonString = ApplicationLogPayloadHelper.NormalizeLogJson(request.LogJson!);
        var gzipBytes = CompressToGzip(jsonString);
        var logDate = ApplicationLogPayloadHelper.ExtractDateOnly(date);
        var pathDate = ApplicationLogPayloadHelper.ToPathSegment(date);
        var remoteBasePath = $"/{pathDate}/";

        var applicationLog = await _applicationLogs.GetByApplicationIdAndDateAsync(appUid, logDate, cancellationToken);
        if (applicationLog is null)
        {
            applicationLog = await _applicationLogs.AddAsync(new ApplicationLogItem
            {
                ApplicationId = appUid,
                Date = logDate,
                RemoteBasePath = remoteBasePath,
                ApplicationName = application.Name ?? string.Empty
            }, cancellationToken);
        }

        var chunkNumber = await _applicationLogChunks.GetChunkCountAsync(applicationLog.Id, cancellationToken) + 1;
        var chunkName = $"logs-{chunkNumber}.gz";

        var uploadResult = await _remoteFileUploadClient.UploadAsync(gzipBytes, chunkName, remoteBasePath, cancellationToken);
        var storedPath = ApplicationLogPayloadHelper.BuildStoredChunkPath(logDate, uploadResult.RemoteName);

        var chunk = await _applicationLogChunks.AddAsync(new ApplicationLogChunkItem
        {
            ApplicationLogId = applicationLog.Id,
            Size = gzipBytes.Length.ToString(),
            Name = chunkName,
            Path = storedPath,
            RemoteName = uploadResult.RemoteName
        }, cancellationToken);

        return new AgentApplicationLogResult
        {
            ApplicationLogId = applicationLog.Id,
            ChunkId = chunk.Id,
            ChunkName = chunkName,
            Path = storedPath,
            Size = chunk.Size
        };
    }

    private static byte[] CompressToGzip(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    public async Task<ApplicationLogListResponse?> GetLogListAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        if (application is null)
            return null;

        var logs = await _applicationLogs.GetByApplicationIdAsync(application.Uid, cancellationToken);
        var dates = new List<ApplicationLogDateItem>();

        foreach (var log in logs)
        {
            var chunks = await _applicationLogChunks.GetByApplicationLogIdAsync(log.Id, cancellationToken);
            dates.Add(new ApplicationLogDateItem
            {
                ApplicationLogId = log.Id,
                Date = log.Date,
                RemoteBasePath = log.RemoteBasePath,
                Chunks = chunks.Select(c => new ApplicationLogChunkDetail
                {
                    Id = c.Id,
                    Name = c.Name,
                    Path = c.Path,
                    Size = c.Size,
                    RemoteName = c.RemoteName
                }).ToList()
            });
        }

        return new ApplicationLogListResponse
        {
            ApplicationId = application.Id,
            AppUid = application.Uid,
            ApplicationName = application.Name ?? string.Empty,
            Dates = dates
        };
    }

    public async Task<ApplicationLogChunkContentResponse?> GetChunkAsync(
        int applicationId,
        string date,
        string? chunk,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        if (application is null)
            return null;

        date = ApplicationLogPayloadHelper.ExtractDateOnly(date.Trim());
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("date is required.");

        var applicationLog = await _applicationLogs.GetByApplicationIdAndDateAsync(application.Uid, date, cancellationToken);
        if (applicationLog is null)
        {
            applicationLog = await FindApplicationLogByDateAsync(application.Uid, date, cancellationToken);
            if (applicationLog is null)
                return null;
        }

        var chunks = await _applicationLogChunks.GetByApplicationLogIdAsync(applicationLog.Id, cancellationToken);
        if (chunks.Count == 0)
            return null;

        var targetChunk = ResolveRequestedChunk(chunks, chunk);
        if (targetChunk is null)
            return null;

        var downloadPath = ApplicationLogPayloadHelper.BuildRemoteDownloadPath(targetChunk.Path, _storagePrefix);
        var gzipBytes = await _remoteFileUploadClient.DownloadAsync(downloadPath, cancellationToken);
        var jsonString = DecompressFromGzip(gzipBytes);
        using var document = JsonDocument.Parse(jsonString);

        var targetIndex = chunks.ToList().FindIndex(c => c.Id == targetChunk.Id);
        var hasNext = targetIndex >= 0 && targetIndex < chunks.Count - 1;

        return new ApplicationLogChunkContentResponse
        {
            ChunkId = targetChunk.Id,
            ChunkName = targetChunk.Name,
            Path = targetChunk.Path,
            Size = targetChunk.Size,
            HasNext = hasNext,
            NextChunk = hasNext ? chunks[targetIndex + 1].Path : null,
            LogJson = document.RootElement.Clone()
        };
    }

    private async Task<ApplicationLogItem?> FindApplicationLogByDateAsync(
        string applicationId,
        string normalizedDate,
        CancellationToken cancellationToken)
    {
        var logs = await _applicationLogs.GetByApplicationIdAsync(applicationId, cancellationToken);
        return logs.FirstOrDefault(log =>
            string.Equals(
                ApplicationLogPayloadHelper.ExtractDateOnly(log.Date),
                normalizedDate,
                StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationLogChunkItem? ResolveRequestedChunk(
        IReadOnlyList<ApplicationLogChunkItem> chunks,
        string? chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk))
            return chunks[0];

        var normalized = chunk.Trim();

        for (var i = 0; i < chunks.Count; i++)
        {
            var item = chunks[i];
            if (ApplicationLogPayloadHelper.ChunkMatches(item, normalized))
                return item;

            if (TryParseChunkNumber(normalized, out var number)
                && TryParseChunkNumber(item.Name, out var chunkNumber)
                && number == chunkNumber)
            {
                return item;
            }
        }

        return null;
    }

    private static bool TryParseChunkNumber(string value, out int number)
    {
        number = 0;
        var normalized = value.Trim();
        if (normalized.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            normalized = Path.GetFileNameWithoutExtension(normalized);

        if (normalized.StartsWith("logs-", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["logs-".Length..];

        return int.TryParse(normalized, out number);
    }

    private static string DecompressFromGzip(byte[] gzipBytes)
    {
        using var input = new MemoryStream(gzipBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
