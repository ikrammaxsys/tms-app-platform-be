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
                domain AS Domain
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
                domain AS Domain
            FROM dbo.Servers
            WHERE id = @Id;
            """;
        return await _sql.QuerySingleAsync<ServerItem>(sql, CommandType.Text, new { Id = id }, null, cancellationToken);
    }

    public async Task<ServerItem> AddAsync(ServerItem server, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Servers (ip_address, environment, internal_external, country, provider, domain)
            OUTPUT
                INSERTED.id AS Id,
                INSERTED.ip_address AS IpAddress,
                INSERTED.environment AS Environment,
                INSERTED.internal_external AS InternalExternal,
                INSERTED.country AS Country,
                INSERTED.provider AS Provider,
                INSERTED.domain AS Domain
            VALUES (@IpAddress, @Environment, @InternalExternal, @Country, @Provider, @Domain);
            """;
        var created = await _sql.QuerySingleAsync<ServerItem>(sql, CommandType.Text, new
        {
            server.IpAddress,
            server.Environment,
            server.InternalExternal,
            server.Country,
            server.Provider,
            server.Domain
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
                domain = @Domain
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
                new SqlParameter("@Domain", server.Domain)
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
}
