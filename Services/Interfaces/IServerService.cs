using tms_template_net8.Models.DTOs.Server;
namespace tms_template_net8.Services;
public interface IServerService
{
    Task<IReadOnlyList<ServerItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServerItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServerItem?> CreateAsync(ServerUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ServerUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
