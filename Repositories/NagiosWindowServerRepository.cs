using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Data.Repositories;

public sealed class NagiosWindowServerRepository : INagiosWindowServerRepository
{
    private readonly ISqlExecutor _sql;

    public NagiosWindowServerRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<NagiosWindowServerItem>> GetLatestPerServerAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                n.id AS Id,
                n.name AS Name,
                n.cpu_count AS CpuCount,
                n.cpu_usage AS CpuUsage,
                n.total_memory AS TotalMemory,
                n.used_memory AS UsedMemory,
                n.total_disk_c AS TotalDiskC,
                n.used_disk_c AS UsedDiskC,
                n.total_disk_d AS TotalDiskD,
                n.used_disk_d AS UsedDiskD,
                n.uptime AS Uptime,
                n.[timestamp] AS Timestamp
            FROM dbo.nagios_window_server n
            INNER JOIN (
                SELECT MAX(id) AS max_id
                FROM dbo.nagios_window_server
                GROUP BY name
            ) latest ON n.id = latest.max_id
            ORDER BY n.name;
            """;

        var rows = await _sql.QueryAsync<NagiosWindowServerItem>(
            sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }
}
