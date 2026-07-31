using tms_template_net8.Models.DTOs.Server;
namespace tms_template_net8.Data.Repositories;
public interface IServerRepository
{
    Task<IReadOnlyList<ServerItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServerItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServerItem?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<ServerItem> AddAsync(ServerItem server, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ServerItem server, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
