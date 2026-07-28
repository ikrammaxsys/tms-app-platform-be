using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public sealed class ApplicationGroupRepository : IApplicationGroupRepository
{
    private readonly ISqlExecutor _sql;
    public ApplicationGroupRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }
    public async Task<IReadOnlyList<ApplicationGroupItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id, name AS Name
            FROM dbo.Application_Groups
            ORDER BY id;
            """;
        var rows = await _sql.QueryAsync<ApplicationGroupItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<ApplicationGroupItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id, name AS Name
            FROM dbo.Application_Groups
            WHERE id = @Id;
            """;
        return await _sql.QuerySingleAsync<ApplicationGroupItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }
    
    public async Task<ApplicationGroupItem> AddAsync(ApplicationGroupItem group, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Application_Groups (name)
            OUTPUT INSERTED.id AS Id, INSERTED.name AS Name
            VALUES (@Name);
            """;
        var created = await _sql.QuerySingleAsync<ApplicationGroupItem>(sql, CommandType.Text, new { group.Name }, null, cancellationToken);
        return created ?? throw new InvalidOperationException("Failed to insert application group.");
    }

    public async Task<bool> UpdateAsync(int id, ApplicationGroupItem group, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.Application_Groups SET name = @Name WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", group.Name)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }
    
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Application_Groups WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }
}
