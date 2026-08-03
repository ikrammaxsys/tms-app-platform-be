using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Agent;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private const string AgentTokenHeader = "X-Agent-Token";
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _agentService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AgentItem>>.SuccessResponse(items, "Agents fetched successfully."));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var rows = (await _agentService.GetAllAsync(cancellationToken))
            .Select(x => new
            {
                x.Id,
                x.Uid,
                x.Name,
                x.ServerId,
                x.ServerDomain,
                x.ServerEnvironment,
                x.Status,
                LastReadyAt = x.LastReadyAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _agentService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<AgentItem>.FailureResponse("Agent not found."));
        return Ok(ApiResponse<AgentItem>.SuccessResponse(item, "Agent fetched successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgentUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse<AgentItem>.FailureResponse("Agent name is required."));
        if (body.ServerId is null or <= 0)
            return BadRequest(ApiResponse<AgentItem>.FailureResponse("Server is required."));

        try
        {
            var created = await _agentService.CreateAsync(body, cancellationToken);
            if (created is null)
                return BadRequest(ApiResponse<AgentItem>.FailureResponse("Server not found."));

            return Ok(ApiResponse<AgentItem>.SuccessResponse(created, "Agent created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AgentItem>.FailureResponse(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AgentUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse.FailureResponse("Agent name is required."));
        if (body.ServerId is null or <= 0)
            return BadRequest(ApiResponse.FailureResponse("Server is required."));

        if (!await _agentService.UpdateAsync(id, body, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Agent or server not found."));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Agent updated successfully."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await _agentService.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(ApiResponse.FailureResponse("Agent not found."));
        if (!await _agentService.DeleteAsync(id, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Agent not found."));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Agent deleted successfully."));
    }

    /// <summary>
    /// Agent ping endpoint. Called by the deployed agent to confirm readiness.
    /// Requires the X-Agent-Token header matching the agent's auth token.
    /// </summary>
    [HttpPost("{agentUid}/ready")]
    public async Task<IActionResult> MarkReady(string agentUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agentUid))
            return BadRequest(ApiResponse<AgentReadyStatusResponse>.FailureResponse("agentUid is required."));

        Request.Headers.TryGetValue(AgentTokenHeader, out var tokenValues);
        var authToken = tokenValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authToken))
            return Unauthorized(ApiResponse<AgentReadyStatusResponse>.FailureResponse(
                $"Missing {AgentTokenHeader} header."));

        try
        {
            var result = await _agentService.MarkReadyAsync(agentUid, authToken, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<AgentReadyStatusResponse>.FailureResponse("Agent not found."));

            return Ok(ApiResponse<AgentReadyStatusResponse>.SuccessResponse(result, "Agent marked as ready."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AgentReadyStatusResponse>.FailureResponse(ex.Message));
        }
    }

    /// <summary>
    /// Platform UI endpoint. Returns the current readiness status for an agent.
    /// </summary>
    [HttpGet("{agentUid}/ready")]
    public async Task<IActionResult> GetReadyStatus(string agentUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agentUid))
            return BadRequest(ApiResponse<AgentReadyStatusResponse>.FailureResponse("agentUid is required."));

        var result = await _agentService.GetReadyStatusAsync(agentUid, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<AgentReadyStatusResponse>.FailureResponse("Agent not found."));

        return Ok(ApiResponse<AgentReadyStatusResponse>.SuccessResponse(result, "Agent readiness status fetched."));
    }

    /// <summary>
    /// Returns the agent configuration JSON for a given agent UID.
    /// </summary>
    [HttpGet("{agentUid}/config")]
    public async Task<IActionResult> GetConfig(string agentUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agentUid))
            return BadRequest(ApiResponse<AgentConfigResponse>.FailureResponse("agentUid is required."));

        var result = await _agentService.GetConfigAsync(agentUid, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<AgentConfigResponse>.FailureResponse("Agent not found."));

        return Ok(ApiResponse<AgentConfigResponse>.SuccessResponse(result, "Agent config fetched successfully."));
    }

    /// <summary>
    /// Updates the agent configuration JSON for a given agent UID.
    /// </summary>
    [HttpPut("{agentUid}/config")]
    public async Task<IActionResult> UpdateConfig(
        string agentUid,
        [FromBody] AgentConfigUpdateRequest? body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agentUid))
            return BadRequest(ApiResponse<AgentConfigResponse>.FailureResponse("agentUid is required."));
        if (body is null)
            return BadRequest(ApiResponse<AgentConfigResponse>.FailureResponse("Request body is required."));

        try
        {
            var result = await _agentService.UpdateConfigAsync(agentUid, body, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<AgentConfigResponse>.FailureResponse("Agent not found."));

            return Ok(ApiResponse<AgentConfigResponse>.SuccessResponse(result, "Agent config updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AgentConfigResponse>.FailureResponse(ex.Message));
        }
    }
}
