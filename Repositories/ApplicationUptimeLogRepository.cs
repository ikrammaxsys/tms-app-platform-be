using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public sealed class ApplicationUptimeLogRepository : IApplicationUptimeLogRepository
{
    private readonly ISqlExecutor _sql;
    public ApplicationUptimeLogRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }
    public async Task<IReadOnlyList<ApplicationUptimeLogItem>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                id_application AS ApplicationId,
                latency AS Latency,
                status AS Status,
                [timestamp] AS Timestamp
            FROM dbo.application_uptime_logs
            WHERE id_application = @ApplicationId
            ORDER BY [timestamp];
            """;
        var rows = await _sql.QueryAsync<ApplicationUptimeLogItem>(
            sql, CommandType.Text, new { ApplicationId = applicationId }, null, cancellationToken);
        return rows.ToList();
    }
    
    public async Task<IReadOnlyList<ApplicationUptimeLogItem>> GetByApplicationIdSinceAsync(
        int applicationId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                id_application AS ApplicationId,
                latency AS Latency,
                status AS Status,
                [timestamp] AS Timestamp
            FROM dbo.application_uptime_logs
            WHERE id_application = @ApplicationId
              AND [timestamp] >= @Since
            ORDER BY [timestamp];
            """;
        var rows = await _sql.QueryAsync<ApplicationUptimeLogItem>(
            sql, CommandType.Text, new { ApplicationId = applicationId, Since = since }, null, cancellationToken);
        return rows.ToList();
    }
}
