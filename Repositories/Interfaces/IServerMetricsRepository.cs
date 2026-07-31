using tms_template_net8.Models.DTOs.Server;

namespace tms_template_net8.Data.Repositories;

public interface IServerMetricsRepository
{
    Task<ServerMetricsItem> AddAsync(ServerMetricsItem metrics, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServerMetricsItem>> GetByServerIdSinceAsync(
        int serverId,
        DateTime since,
        CancellationToken cancellationToken = default);
    Task<ServerMetricsItem?> GetLatestByServerIdAsync(
        int serverId,
        CancellationToken cancellationToken = default);
}
