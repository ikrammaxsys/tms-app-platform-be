using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Common.Time;
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

    public async Task<IReadOnlyList<ApplicationUptimeLogItem>> GetByApplicationIdBetweenAsync(
        int applicationId,
        DateTime from,
        DateTime toExclusive,
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
              AND [timestamp] >= @From
              AND [timestamp] < @ToExclusive
            ORDER BY [timestamp];
            """;
        var rows = await _sql.QueryAsync<ApplicationUptimeLogItem>(
            sql,
            CommandType.Text,
            new { ApplicationId = applicationId, From = from, ToExclusive = toExclusive },
            null,
            cancellationToken);
        return rows.ToList();
    }

    public async Task<ApplicationUptimeLogItem?> GetLatestByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                id AS Id,
                id_application AS ApplicationId,
                latency AS Latency,
                status AS Status,
                [timestamp] AS Timestamp
            FROM dbo.application_uptime_logs
            WHERE id_application = @ApplicationId
            ORDER BY [timestamp] DESC, id DESC;
            """;
        return await _sql.QuerySingleAsync<ApplicationUptimeLogItem>(
            sql, CommandType.Text, new { ApplicationId = applicationId }, null, cancellationToken);
    }

    public async Task<ApplicationUptimeLogItem> AddAsync(
        ApplicationUptimeLogItem log,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.application_uptime_logs
                (id_application, latency, status, [timestamp])
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.id_application AS ApplicationId,
                INSERTED.latency AS Latency,
                INSERTED.status AS Status,
                INSERTED.[timestamp] AS Timestamp
            VALUES
                (@ApplicationId, @Latency, @Status, @Timestamp);
            """;
        var inserted = await _sql.QuerySingleAsync<ApplicationUptimeLogItem>(sql, CommandType.Text, new
        {
            log.ApplicationId,
            Latency = log.Latency,
            Status = string.IsNullOrWhiteSpace(log.Status) ? "Up" : log.Status,
            Timestamp = MalaysiaTime.ForStorage(log.Timestamp)
        }, null, cancellationToken);
        if (inserted is null)
            throw new InvalidOperationException("Failed to insert application uptime log.");
        return inserted;
    }
}
