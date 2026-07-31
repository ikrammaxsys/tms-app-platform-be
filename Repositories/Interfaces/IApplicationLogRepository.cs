using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Data.Repositories;

public interface IApplicationLogRepository
{
    Task<ApplicationLogItem?> GetByApplicationIdAndDateAsync(
        string applicationId,
        string date,
        CancellationToken cancellationToken = default);

    Task<ApplicationLogItem> AddAsync(
        ApplicationLogItem log,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationLogItem>> GetByApplicationIdAsync(
        string applicationId,
        CancellationToken cancellationToken = default);
}
