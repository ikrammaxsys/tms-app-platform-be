using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Common.Time;
using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly ISqlExecutor _sql;
    private const string SelectSql = """
        SELECT
            a.id AS Id,
            a.uid AS Uid,
            a.name AS Name,
            a.version AS Version,
            a.commit_id AS [Commit],
            a.status AS Status,
            a.last_deployment AS LastDeployment,
            a.app_url AS AppUrl,
            a.repository_url AS RepositoryUrl,
            a.id_server AS ServerId,
            a.id_application_group AS ApplicationGroupId,
            s.domain AS ServerDomain,
            s.environment AS ServerEnvironment,
            s.ip_address AS ServerIpAddress,
            g.name AS ApplicationGroupName
        FROM dbo.Applications a
        INNER JOIN dbo.Servers s ON s.id = a.id_server
        INNER JOIN dbo.Application_Groups g ON g.id = a.id_application_group
        """;

    public ApplicationRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<ApplicationItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " ORDER BY a.id;";
        var rows = await _sql.QueryAsync<ApplicationItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<ApplicationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE a.id = @Id;";
        return await _sql.QuerySingleAsync<ApplicationItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<ApplicationItem?> GetByUidAsync(string uid, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE a.uid = @Uid;";
        return await _sql.QuerySingleAsync<ApplicationItem>(sql, CommandType.Text, new { Uid = uid }, null, cancellationToken);
    }

    public async Task<bool> UidExistsAsync(string uid, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Applications
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

    public async Task<ApplicationItem> AddAsync(ApplicationItem application, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Applications
                (uid, name, version, commit_id, status, last_deployment, app_url, repository_url, id_server, id_application_group)
            OUTPUT INSERTED.id AS Id
            VALUES
                (@Uid, @Name, @Version, @Commit, @Status, @LastDeployment, @AppUrl, @RepositoryUrl, @ServerId, @ApplicationGroupId);
            """;
        var inserted = await _sql.QuerySingleAsync<ApplicationItem>(sql, CommandType.Text, new
        {
            application.Uid,
            application.Name,
            application.Version,
            application.Commit,
            application.Status,
            LastDeployment = application.LastDeployment.HasValue
                ? MalaysiaTime.ForStorage(application.LastDeployment)
                : (DateTime?)null,
            application.AppUrl,
            application.RepositoryUrl,
            application.ServerId,
            application.ApplicationGroupId
        }, null, cancellationToken);
        if (inserted is null)
            throw new InvalidOperationException("Failed to insert application.");
        return (await GetByIdAsync(inserted.Id, cancellationToken))
            ?? throw new InvalidOperationException("Failed to load inserted application.");
    }

    public async Task<bool> UpdateAsync(int id, ApplicationItem application, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Applications
            SET
                uid = @Uid,
                name = @Name,
                version = @Version,
                commit_id = @Commit,
                status = @Status,
                last_deployment = @LastDeployment,
                app_url = @AppUrl,
                repository_url = @RepositoryUrl,
                id_server = @ServerId,
                id_application_group = @ApplicationGroupId
            WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@Uid", application.Uid),
                new SqlParameter("@Name", application.Name),
                new SqlParameter("@Version", application.Version),
                new SqlParameter("@Commit", (object?)application.Commit ?? DBNull.Value),
                new SqlParameter("@Status", application.Status),
                new SqlParameter("@LastDeployment", application.LastDeployment.HasValue
                    ? MalaysiaTime.ForStorage(application.LastDeployment)
                    : DBNull.Value),
                new SqlParameter("@AppUrl", (object?)application.AppUrl ?? DBNull.Value),
                new SqlParameter("@RepositoryUrl", (object?)application.RepositoryUrl ?? DBNull.Value),
                new SqlParameter("@ServerId", application.ServerId),
                new SqlParameter("@ApplicationGroupId", application.ApplicationGroupId)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> UpdateCurrentDeploymentAsync(
        int id,
        string version,
        string commit,
        DateTime? lastDeployment,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Applications
            SET
                version = @Version,
                commit_id = @Commit,
                last_deployment = @LastDeployment
            WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@Version", version),
                new SqlParameter("@Commit", (object?)commit ?? DBNull.Value),
                new SqlParameter("@LastDeployment", MalaysiaTime.ForStorage(lastDeployment))
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM dbo.application_uptime_logs WHERE id_application = @Id;
            DELETE FROM dbo.Application_deployments WHERE application_id = @Id;
            DELETE FROM dbo.Applications WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> AnyByServerIdAsync(int serverId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Applications WHERE id_server = @ServerId
            ) THEN 1 ELSE 0 END AS bit);
            """;
        return await _sql.QuerySingleAsync<bool>(sql, CommandType.Text, new { ServerId = serverId }, null, cancellationToken);
    }

    public async Task<bool> AnyByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Applications WHERE id_application_group = @GroupId
            ) THEN 1 ELSE 0 END AS bit);
            """;
        return await _sql.QuerySingleAsync<bool>(sql, CommandType.Text, new { GroupId = groupId }, null, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationItem>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE a.id_application_group = @GroupId;";
        var rows = await _sql.QueryAsync<ApplicationItem>(sql, CommandType.Text, new { GroupId = groupId }, null, cancellationToken);
        return rows.ToList();
    }
}
