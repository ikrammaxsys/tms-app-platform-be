using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public interface IApplicationUptimeLogRepository
{
    Task<IReadOnlyList<ApplicationUptimeLogItem>> GetByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUptimeLogItem>> GetByApplicationIdSinceAsync(int applicationId, DateTime since, CancellationToken cancellationToken = default);
    Task<ApplicationUptimeLogItem?> GetLatestByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationUptimeLogItem> AddAsync(ApplicationUptimeLogItem log, CancellationToken cancellationToken = default);
}
