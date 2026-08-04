using System.Data;
using Microsoft.Data.SqlClient;
using TMS.WebApp.Sdk.Data.Sql;
using tms_template_net8.Models.DTOs.Server;
namespace tms_template_net8.Data.Repositories;
public sealed class ServerRepository : IServerRepository
{
    private readonly ISqlExecutor _sql;
    public ServerRepository(ISqlExecutor sql)
    {
        _sql = sql;
    }
    public async Task<IReadOnlyList<ServerItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                ip_address AS IpAddress,
                environment AS Environment,
                internal_external AS InternalExternal,
                country AS Country,
                provider AS Provider,
                domain AS Domain,
                organization_id AS OrganizationId
            FROM dbo.Servers
            ORDER BY id;
            """;
        var rows = await _sql.QueryAsync<ServerItem>(sql, CommandType.Text, null, null, cancellationToken);
        return rows.ToList();
    }

    public async Task<ServerItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                ip_address AS IpAddress,
                environment AS Environment,
                internal_external AS InternalExternal,
                country AS Country,
                provider AS Provider,
                domain AS Domain,
                organization_id AS OrganizationId
            FROM dbo.Servers
            WHERE id = @Id;
            """;
        return await _sql.QuerySingleAsync<ServerItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<ServerItem?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                ip_address AS IpAddress,
                environment AS Environment,
                internal_external AS InternalExternal,
                country AS Country,
                provider AS Provider,
                domain AS Domain,
                organization_id AS OrganizationId
            FROM dbo.Servers
            WHERE ip_address = @IpAddress;
            """;
        return await _sql.QuerySingleAsync<ServerItem>(
            sql, CommandType.Text, new { IpAddress = ipAddress.Trim() }, null, cancellationToken);
    }

    public async Task<ServerItem> AddAsync(ServerItem server, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Servers (ip_address, environment, internal_external, country, provider, domain, organization_id)
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.ip_address AS IpAddress,
                INSERTED.environment AS Environment,
                INSERTED.internal_external AS InternalExternal,
                INSERTED.country AS Country,
                INSERTED.provider AS Provider,
                INSERTED.domain AS Domain,
                INSERTED.organization_id AS OrganizationId
            VALUES (@IpAddress, @Environment, @InternalExternal, @Country, @Provider, @Domain, @OrganizationId);
            """;
        var created = await _sql.QuerySingleAsync<ServerItem>(sql, CommandType.Text, new
        {
            server.IpAddress,
            server.Environment,
            server.InternalExternal,
            server.Country,
            server.Provider,
            server.Domain,
            OrganizationId = (object?)server.OrganizationId ?? DBNull.Value
        }, null, cancellationToken);
        return created ?? throw new InvalidOperationException("Failed to insert server.");
    }

    public async Task<bool> UpdateAsync(int id, ServerItem server, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Servers
            SET
                ip_address = @IpAddress,
                environment = @Environment,
                internal_external = @InternalExternal,
                country = @Country,
                provider = @Provider,
                domain = @Domain,
                organization_id = @OrganizationId
            WHERE id = @Id;
            """;
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [
                new SqlParameter("@Id", id),
                new SqlParameter("@IpAddress", server.IpAddress),
                new SqlParameter("@Environment", server.Environment),
                new SqlParameter("@InternalExternal", server.InternalExternal),
                new SqlParameter("@Country", (object?)server.Country ?? DBNull.Value),
                new SqlParameter("@Provider", (object?)server.Provider ?? DBNull.Value),
                new SqlParameter("@Domain", server.Domain),
                new SqlParameter("@OrganizationId", (object?)server.OrganizationId ?? DBNull.Value)
            ],
            null,
            cancellationToken);
        return affected > 0;
    }
    
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Servers WHERE id = @Id;";
        var affected = await _sql.ExecuteNonQueryAsync(
            sql,
            CommandType.Text,
            [new SqlParameter("@Id", id)],
            null,
            cancellationToken);
        return affected > 0;
    }

    public async Task<bool> AnyByOrganizationIdAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Servers WHERE organization_id = @OrganizationId
            ) THEN 1 ELSE 0 END AS bit);
            """;
        return await _sql.QuerySingleAsync<bool>(
            sql, CommandType.Text, new { OrganizationId = organizationId }, null, cancellationToken);
    }
}
