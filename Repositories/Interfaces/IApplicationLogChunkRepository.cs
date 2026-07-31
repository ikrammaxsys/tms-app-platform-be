using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Data.Repositories;

public interface IApplicationLogChunkRepository
{
    Task<int> GetChunkCountAsync(int applicationLogId, CancellationToken cancellationToken = default);

    Task<ApplicationLogChunkItem> AddAsync(
        ApplicationLogChunkItem chunk,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationLogChunkItem>> GetByApplicationLogIdAsync(
        int applicationLogId,
        CancellationToken cancellationToken = default);
}
