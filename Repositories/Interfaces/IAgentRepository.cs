using tms_template_net8.Models.DTOs.Agent;

namespace tms_template_net8.Data.Repositories;

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AgentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AgentItem?> GetByUidAsync(string uid, CancellationToken cancellationToken = default);
    Task<bool> UidExistsAsync(string uid, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<AgentItem> AddAsync(AgentItem agent, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, AgentItem agent, CancellationToken cancellationToken = default);
    Task<bool> MarkReadyAsync(string uid, DateTime readyAt, CancellationToken cancellationToken = default);
    Task<bool> UpdateConfigAsync(string uid, string? configJson, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
