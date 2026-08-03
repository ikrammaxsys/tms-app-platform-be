using System.Text.Json;
using tms_template_net8.Common.Time;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Agent;

namespace tms_template_net8.Services;

public sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _repository;
    private readonly IServerRepository _serverRepository;

    public AgentService(IAgentRepository repository, IServerRepository serverRepository)
    {
        _repository = repository;
        _serverRepository = serverRepository;
    }

    public Task<IReadOnlyList<AgentItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<AgentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<AgentItem?> GetByUidAsync(string uid, CancellationToken cancellationToken = default) =>
        _repository.GetByUidAsync(uid, cancellationToken);

    public async Task<AgentItem?> CreateAsync(AgentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ServerId is null or <= 0)
            return null;

        if (await _serverRepository.GetByIdAsync(request.ServerId.Value, cancellationToken) is null)
            return null;

        var agent = ToEntity(request);
        if (await _repository.UidExistsAsync(agent.Uid, null, cancellationToken))
            throw new InvalidOperationException($"Agent uid '{agent.Uid}' already exists.");

        return await _repository.AddAsync(agent, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, AgentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return false;
        if (request.ServerId is null or <= 0)
            return false;
        if (await _serverRepository.GetByIdAsync(request.ServerId.Value, cancellationToken) is null)
            return false;

        existing.Name = (request.Name ?? string.Empty).Trim();
        existing.ServerId = request.ServerId.Value;
        return await _repository.UpdateAsync(id, existing, cancellationToken);
    }

    public async Task<AgentReadyStatusResponse?> MarkReadyAsync(
        string agentUid,
        string? authToken,
        CancellationToken cancellationToken = default)
    {
        var uid = (agentUid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        var agent = await _repository.GetByUidAsync(uid, cancellationToken);
        if (agent is null)
            return null;

        if (!string.IsNullOrWhiteSpace(authToken)
            && !string.Equals(agent.AuthToken, authToken.Trim(), StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Invalid agent authentication token.");

        var readyAt = MalaysiaTime.Now;
        if (!await _repository.MarkReadyAsync(uid, readyAt, cancellationToken))
            return null;

        return ToReadyStatus(agent, "Ready", readyAt);
    }

    public async Task<AgentReadyStatusResponse?> GetReadyStatusAsync(
        string agentUid,
        CancellationToken cancellationToken = default)
    {
        var uid = (agentUid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        var agent = await _repository.GetByUidAsync(uid, cancellationToken);
        return agent is null ? null : ToReadyStatus(agent);
    }

    public async Task<AgentConfigResponse?> GetConfigAsync(
        string agentUid,
        CancellationToken cancellationToken = default)
    {
        var uid = (agentUid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        var agent = await _repository.GetByUidAsync(uid, cancellationToken);
        return agent is null ? null : ToConfigResponse(agent);
    }

    public async Task<AgentConfigResponse?> UpdateConfigAsync(
        string agentUid,
        AgentConfigUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var uid = (agentUid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        if (await _repository.GetByUidAsync(uid, cancellationToken) is null)
            return null;

        var configJson = NormalizeConfigJson(request.ConfigJson);
        if (!await _repository.UpdateConfigAsync(uid, configJson, cancellationToken))
            return null;

        return new AgentConfigResponse
        {
            AgentUid = uid,
            ConfigJson = configJson ?? "{}"
        };
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    private static string GenerateAgentUid() => $"agent-{Guid.NewGuid():N}"[..14];

    private static string GenerateAuthToken() => $"tms_{Guid.NewGuid():N}";

    private static AgentItem ToEntity(AgentUpsertRequest request)
    {
        var uid = (request.Uid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            uid = GenerateAgentUid();

        var authToken = (request.AuthToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(authToken))
            authToken = GenerateAuthToken();

        return new AgentItem
        {
            Uid = uid,
            Name = (request.Name ?? string.Empty).Trim(),
            ServerId = request.ServerId!.Value,
            AuthToken = authToken,
            Status = "Pending",
            CreatedAt = MalaysiaTime.Now
        };
    }

    private static AgentReadyStatusResponse ToReadyStatus(
        AgentItem agent,
        string? statusOverride = null,
        DateTime? lastReadyAtOverride = null) => new()
    {
        AgentUid = agent.Uid,
        Name = agent.Name,
        Status = statusOverride ?? agent.Status,
        LastReadyAt = lastReadyAtOverride ?? agent.LastReadyAt,
        ServerDomain = agent.ServerDomain
    };

    private static AgentConfigResponse ToConfigResponse(AgentItem agent) => new()
    {
        AgentUid = agent.Uid,
        ConfigJson = string.IsNullOrWhiteSpace(agent.ConfigJson) ? "{}" : agent.ConfigJson
    };

    private static string? NormalizeConfigJson(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return null;

        var trimmed = configJson.Trim();
        try
        {
            using var _ = JsonDocument.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"configJson must be valid JSON. {ex.Message}");
        }

        return trimmed;
    }
}
