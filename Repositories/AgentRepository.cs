using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Agent;

namespace tms_template_net8.Data.Repositories;

public sealed class AgentRepository : IAgentRepository
{
    private readonly ISqlExecutor _sql;

    private const string SelectSql = """
        SELECT
            a.id AS Id,
            a.uid AS Uid,
            a.name AS Name,
            a.id_server AS ServerId,
            a.auth_token AS AuthToken,
            a.status AS Status,
            a.last_ready_at AS LastReadyAt,
            a.created_at AS CreatedAt,
            a.config_json AS ConfigJson,
            s.domain AS ServerDomain,
            s.environment AS ServerEnvironment
        FROM dbo.agents a
        INNER JOIN dbo.Servers s ON s.id = a.id_server
        """;

    public AgentRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<AgentItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " ORDER BY a.id;";
        var rows = await _sql.QueryAsync<AgentItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<AgentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE a.id = @Id;";
        return await _sql.QuerySingleAsync<AgentItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<AgentItem?> GetByUidAsync(string uid, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE a.uid = @Uid;";
        return await _sql.QuerySingleAsync<AgentItem>(
            sql, CommandType.Text, new { Uid = uid.Trim() }, null, cancellationToken);
    }

    public async Task<bool> UidExistsAsync(string uid, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Agents
                WHERE uid = @Uid
                  AND (@ExcludeId IS NULL OR id <> @ExcludeId)
            ) THEN 1 ELSE 0 END AS bit);
            """;
        return await _sql.QuerySingleAsync<bool>(
            sql,
            CommandType.Text,
            new { Uid = uid, ExcludeId = excludeId },
            null,
            cancellationToken);
    }

    public async Task<AgentItem> AddAsync(AgentItem agent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.agents (uid, name, id_server, auth_token, status, last_ready_at, created_at)
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.uid AS Uid,
                INSERTED.name AS Name,
                INSERTED.id_server AS ServerId,
                INSERTED.auth_token AS AuthToken,
                INSERTED.status AS Status,
                INSERTED.last_ready_at AS LastReadyAt,
                INSERTED.created_at AS CreatedAt
            VALUES (@Uid, @Name, @ServerId, @AuthToken, @Status, @LastReadyAt, @CreatedAt);
            """;
        var created = await _sql.QuerySingleAsync<AgentItem>(sql, CommandType.Text, new
        {
            agent.Uid,
            agent.Name,
            agent.ServerId,
            agent.AuthToken,
            agent.Status,
            LastReadyAt = (object?)agent.LastReadyAt ?? DBNull.Value,
            agent.CreatedAt
        }, null, cancellationToken);

        var result = created ?? throw new InvalidOperationException("Failed to insert agent.");
        var withServer = await GetByIdAsync(result.Id, cancellationToken);
        return withServer ?? result;
    }

    public async Task<bool> UpdateAsync(int id, AgentItem agent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.agents
            SET
                name = @Name,
                id_server = @ServerId
            WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", agent.Name),
                new SqlParameter("@ServerId", agent.ServerId)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> MarkReadyAsync(string uid, DateTime readyAt, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.agents
            SET
                status = N'Ready',
                last_ready_at = @LastReadyAt
            WHERE uid = @Uid;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Uid", uid.Trim()),
                new SqlParameter("@LastReadyAt", readyAt)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> UpdateConfigAsync(string uid, string? configJson, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.agents
            SET config_json = @ConfigJson
            WHERE uid = @Uid;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Uid", uid.Trim()),
                new SqlParameter("@ConfigJson", (object?)configJson ?? DBNull.Value)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.agents WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }
}
