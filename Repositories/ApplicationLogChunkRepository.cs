using System.Data;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.ApplicationLogs;

namespace tms_template_net8.Data.Repositories;

public sealed class ApplicationLogChunkRepository : IApplicationLogChunkRepository
{
    private readonly ISqlExecutor _sql;

    public ApplicationLogChunkRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<int> GetChunkCountAsync(
        int applicationLogId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.application_log_chunks
            WHERE application_log_id = @ApplicationLogId;
            """;
        return await _sql.QuerySingleAsync<int>(
            sql,
            CommandType.Text,
            new { ApplicationLogId = applicationLogId },
            null,
            cancellationToken);
    }

    public async Task<ApplicationLogChunkItem> AddAsync(
        ApplicationLogChunkItem chunk,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.application_log_chunks
                (application_log_id, size, name, path, remote_name)
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.application_log_id AS ApplicationLogId,
                INSERTED.size AS Size,
                INSERTED.name AS Name,
                INSERTED.path AS Path,
                INSERTED.remote_name AS RemoteName
            VALUES
                (@ApplicationLogId, @Size, @Name, @Path, @RemoteName);
            """;
        var inserted = await _sql.QuerySingleAsync<ApplicationLogChunkItem>(
            sql,
            CommandType.Text,
            new
            {
                chunk.ApplicationLogId,
                chunk.Size,
                chunk.Name,
                chunk.Path,
                chunk.RemoteName
            },
            null,
            cancellationToken);
        if (inserted is null)
            throw new InvalidOperationException("Failed to insert application log chunk.");
        return inserted;
    }

    public async Task<IReadOnlyList<ApplicationLogChunkItem>> GetByApplicationLogIdAsync(
        int applicationLogId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                application_log_id AS ApplicationLogId,
                size AS Size,
                name AS Name,
                path AS Path,
                remote_name AS RemoteName
            FROM dbo.application_log_chunks
            WHERE application_log_id = @ApplicationLogId
            ORDER BY id;
            """;
        var rows = await _sql.QueryAsync<ApplicationLogChunkItem>(
            sql,
            CommandType.Text,
            new { ApplicationLogId = applicationLogId },
            null,
            cancellationToken);
        return rows.ToList();
    }
}
