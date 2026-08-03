using tms_template_net8.Models.DTOs.Agent;

namespace tms_template_net8.Services;

public interface IAgentService
{
    Task<IReadOnlyList<AgentItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AgentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AgentItem?> GetByUidAsync(string uid, CancellationToken cancellationToken = default);
    Task<AgentItem?> CreateAsync(AgentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, AgentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<AgentReadyStatusResponse?> MarkReadyAsync(string agentUid, string? authToken, CancellationToken cancellationToken = default);
    Task<AgentReadyStatusResponse?> GetReadyStatusAsync(string agentUid, CancellationToken cancellationToken = default);
    Task<AgentConfigResponse?> GetConfigAsync(string agentUid, CancellationToken cancellationToken = default);
    Task<AgentConfigResponse?> UpdateConfigAsync(string agentUid, AgentConfigUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
