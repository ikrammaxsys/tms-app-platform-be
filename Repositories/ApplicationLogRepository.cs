using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Data.Repositories;

public sealed class ApplicationLogRepository : IApplicationLogRepository
{
    private readonly ISqlExecutor _sql;

    public ApplicationLogRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<ApplicationLogItem?> GetByApplicationIdAndDateAsync(
        string applicationId,
        string date,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                application_id AS ApplicationId,
                [date] AS Date,
                remote_base_path AS RemoteBasePath,
                application_name AS ApplicationName
            FROM dbo.application_logs
            WHERE application_id = @ApplicationId
              AND [date] = @Date;
            """;
        return await _sql.QuerySingleAsync<ApplicationLogItem>(
            sql,
            CommandType.Text,
            new { ApplicationId = applicationId, Date = date },
            null,
            cancellationToken);
    }

    public async Task<ApplicationLogItem> AddAsync(
        ApplicationLogItem log,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.application_logs
                (application_id, [date], remote_base_path, application_name)
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.application_id AS ApplicationId,
                INSERTED.[date] AS Date,
                INSERTED.remote_base_path AS RemoteBasePath,
                INSERTED.application_name AS ApplicationName
            VALUES
                (@ApplicationId, @Date, @RemoteBasePath, @ApplicationName);
            """;
        var inserted = await _sql.QuerySingleAsync<ApplicationLogItem>(
            sql,
            CommandType.Text,
            new
            {
                log.ApplicationId,
                log.Date,
                log.RemoteBasePath,
                log.ApplicationName
            },
            null,
            cancellationToken);
        if (inserted is null)
            throw new InvalidOperationException("Failed to insert application log.");
        return inserted;
    }

    public async Task<IReadOnlyList<ApplicationLogItem>> GetByApplicationIdAsync(
        string applicationId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                application_id AS ApplicationId,
                [date] AS Date,
                remote_base_path AS RemoteBasePath,
                application_name AS ApplicationName
            FROM dbo.application_logs
            WHERE application_id = @ApplicationId
            ORDER BY [date] DESC, id DESC;
            """;
        var rows = await _sql.QueryAsync<ApplicationLogItem>(
            sql,
            CommandType.Text,
            new { ApplicationId = applicationId },
            null,
            cancellationToken);
        return rows.ToList();
    }
}
