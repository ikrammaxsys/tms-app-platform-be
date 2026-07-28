using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Application;

namespace tms_template_net8.Data.Repositories;

public sealed class ApplicationDeploymentRepository : IApplicationDeploymentRepository
{
    private readonly ISqlExecutor _sql;

    private const string SelectSql = """
        SELECT
            d.id AS Id,
            d.application_id AS ApplicationId,
            d.commit_no AS CommitNo,
            d.version AS Version,
            d.[timestamp] AS [Timestamp],
            ISNULL(a.name, '') AS ApplicationName
        FROM dbo.Application_deployments d
        LEFT JOIN dbo.Applications a ON a.id = d.application_id
        """;

    public ApplicationDeploymentRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<ApplicationDeploymentItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " ORDER BY d.id DESC;";
        var rows = await _sql.QueryAsync<ApplicationDeploymentItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ApplicationDeploymentItem>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE d.application_id = @ApplicationId ORDER BY d.id DESC;";
        var rows = await _sql.QueryAsync<ApplicationDeploymentItem>(
            sql, CommandType.Text, new { ApplicationId = applicationId }, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<ApplicationDeploymentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = SelectSql + " WHERE d.id = @Id;";
        return await _sql.QuerySingleAsync<ApplicationDeploymentItem>(
            sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<ApplicationDeploymentItem> AddAsync(
        ApplicationDeploymentItem deployment,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Application_deployments
                (application_id, commit_no, version, [timestamp])
            OUTPUT INSERTED.id AS Id
            VALUES
                (@ApplicationId, @CommitNo, @Version, @Timestamp);
            """;
        var inserted = await _sql.QuerySingleAsync<ApplicationDeploymentItem>(sql, CommandType.Text, new
        {
            deployment.ApplicationId,
            deployment.CommitNo,
            deployment.Version,
            deployment.Timestamp
        }, null, cancellationToken);
        if (inserted is null)
            throw new InvalidOperationException("Failed to insert application deployment.");
        return (await GetByIdAsync(inserted.Id, cancellationToken))
            ?? throw new InvalidOperationException("Failed to load inserted application deployment.");
    }

    public async Task<bool> UpdateAsync(
        int id,
        ApplicationDeploymentItem deployment,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Application_deployments
            SET
                application_id = @ApplicationId,
                commit_no = @CommitNo,
                version = @Version,
                [timestamp] = @Timestamp
            WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@ApplicationId", deployment.ApplicationId),
                new SqlParameter("@CommitNo", (object?)deployment.CommitNo ?? DBNull.Value),
                new SqlParameter("@Version", (object?)deployment.Version ?? DBNull.Value),
                new SqlParameter("@Timestamp", (object?)deployment.Timestamp ?? DBNull.Value)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Application_deployments WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task DeleteByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Application_deployments WHERE application_id = @ApplicationId;";
        await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@ApplicationId", applicationId)],
            null,
            cancellationToken);
    }
}
