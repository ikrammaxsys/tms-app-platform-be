using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Services;

public interface IApplicationLogService
{
    Task<AgentApplicationLogResult?> IngestAsync(
        AgentApplicationLogRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationLogListResponse?> GetLogListAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationLogChunkContentResponse?> GetChunkAsync(
        int applicationId,
        string date,
        string? chunk,
        CancellationToken cancellationToken = default);
}
