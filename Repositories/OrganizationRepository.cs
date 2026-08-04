using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Organization;

namespace tms_template_net8.Data.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly ISqlExecutor _sql;

    public OrganizationRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<OrganizationItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id, code AS Code, name AS Name
            FROM dbo.organizations
            ORDER BY name;
            """;
        var rows = await _sql.QueryAsync<OrganizationItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<OrganizationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id, code AS Code, name AS Name
            FROM dbo.organizations
            WHERE id = @Id;
            """;
        return await _sql.QuerySingleAsync<OrganizationItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<OrganizationItem> AddAsync(OrganizationItem organization, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.organizations (code, name)
            OUTPUT INSERTED.id AS Id, INSERTED.code AS Code, INSERTED.name AS Name
            VALUES (@Code, @Name);
            """;
        var created = await _sql.QuerySingleAsync<OrganizationItem>(
            sql, CommandType.Text, new { organization.Code, organization.Name }, null, cancellationToken);
        return created ?? throw new InvalidOperationException("Failed to insert organization.");
    }

    public async Task<bool> UpdateAsync(int id, OrganizationItem organization, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE dbo.organizations SET code = @Code, name = @Name WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@Code", organization.Code),
                new SqlParameter("@Name", organization.Name)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.organizations WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }
}
