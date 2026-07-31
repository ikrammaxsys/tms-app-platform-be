using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Common.Time;
using tms_template_net8.Models.DTOs.Server;

namespace tms_template_net8.Data.Repositories;

public sealed class ServerMetricsRepository : IServerMetricsRepository
{
    private readonly ISqlExecutor _sql;

    public ServerMetricsRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<ServerMetricsItem> AddAsync(
        ServerMetricsItem metrics,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.server_metrics
                (server_id, cpu_cores, cpu_usage, ram_total, ram_usage, ram_available,
                 disk_total, disk_used, disk_available, [timestamp])
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.server_id AS ServerId,
                INSERTED.cpu_cores AS CpuCores,
                INSERTED.cpu_usage AS CpuUsage,
                INSERTED.ram_total AS RamTotal,
                INSERTED.ram_usage AS RamUsage,
                INSERTED.ram_available AS RamAvailable,
                INSERTED.disk_total AS DiskTotal,
                INSERTED.disk_used AS DiskUsed,
                INSERTED.disk_available AS DiskAvailable,
                INSERTED.[timestamp] AS Timestamp
            VALUES
                (@ServerId, @CpuCores, @CpuUsage, @RamTotal, @RamUsage, @RamAvailable,
                 @DiskTotal, @DiskUsed, @DiskAvailable, @Timestamp);
            """;

        var inserted = await _sql.QuerySingleAsync<ServerMetricsItem>(sql, CommandType.Text, new
        {
            metrics.ServerId,
            metrics.CpuCores,
            metrics.CpuUsage,
            metrics.RamTotal,
            metrics.RamUsage,
            metrics.RamAvailable,
            metrics.DiskTotal,
            metrics.DiskUsed,
            metrics.DiskAvailable,
            Timestamp = MalaysiaTime.ForStorage(metrics.Timestamp)
        }, null, cancellationToken);

        return inserted ?? throw new InvalidOperationException("Failed to insert server metrics.");
    }

    public async Task<IReadOnlyList<ServerMetricsItem>> GetByServerIdSinceAsync(
        int serverId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                server_id AS ServerId,
                cpu_cores AS CpuCores,
                cpu_usage AS CpuUsage,
                ram_total AS RamTotal,
                ram_usage AS RamUsage,
                ram_available AS RamAvailable,
                disk_total AS DiskTotal,
                disk_used AS DiskUsed,
                disk_available AS DiskAvailable,
                [timestamp] AS Timestamp
            FROM dbo.server_metrics
            WHERE server_id = @ServerId
              AND [timestamp] >= @Since
            ORDER BY [timestamp];
            """;

        var rows = await _sql.QueryAsync<ServerMetricsItem>(
            sql, CommandType.Text, new { ServerId = serverId, Since = since }, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<ServerMetricsItem?> GetLatestByServerIdAsync(
        int serverId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                id AS Id,
                server_id AS ServerId,
                cpu_cores AS CpuCores,
                cpu_usage AS CpuUsage,
                ram_total AS RamTotal,
                ram_usage AS RamUsage,
                ram_available AS RamAvailable,
                disk_total AS DiskTotal,
                disk_used AS DiskUsed,
                disk_available AS DiskAvailable,
                [timestamp] AS Timestamp
            FROM dbo.server_metrics
            WHERE server_id = @ServerId
            ORDER BY [timestamp] DESC, id DESC;
            """;

        return await _sql.QuerySingleAsync<ServerMetricsItem>(
            sql, CommandType.Text, new { ServerId = serverId }, null, cancellationToken);
    }
}
