using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Server;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Api;
[ApiController]
[Route("api/servers")]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;
    public ServersController(IServerService serverService)
    {
        _serverService = serverService;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _serverService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServerItem>>.SuccessResponse(items, "Servers fetched successfully."));
    }
    [HttpGet("list")]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var rows = (await _serverService.GetAllAsync(cancellationToken))
            .Select(x => new
            {
                x.Id,
                x.Domain,
                x.IpAddress,
                x.Environment,
                x.InternalExternal,
                x.Country,
                x.Provider,
                x.OrganizationId
            })
            .ToList();
        return Ok(rows);
    }
    [HttpGet("options")]
    public async Task<IActionResult> ServerOptions(CancellationToken cancellationToken)
    {
        var options = (await _serverService.GetAllAsync(cancellationToken))
            .Select(x => new { value = x.Id.ToString(), text = $"{x.Domain} ({x.Environment})" })
            .ToList();
        return Ok(options);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _serverService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<ServerItem>.FailureResponse("Server not found."));
        return Ok(ApiResponse<ServerItem>.SuccessResponse(item, "Server fetched successfully."));
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServerUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Domain))
            return BadRequest(ApiResponse<ServerItem>.FailureResponse("Domain is required."));
        if (string.IsNullOrWhiteSpace(body.IpAddress))
            return BadRequest(ApiResponse<ServerItem>.FailureResponse("IP address is required."));
        var created = await _serverService.CreateAsync(body, cancellationToken);
        return Ok(ApiResponse<ServerItem>.SuccessResponse(created, "Server created successfully."));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServerUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Domain))
            return BadRequest(ApiResponse.FailureResponse("Domain is required."));
        if (string.IsNullOrWhiteSpace(body.IpAddress))
            return BadRequest(ApiResponse.FailureResponse("IP address is required."));
        if (!await _serverService.UpdateAsync(id, body, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Server not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Server updated successfully."));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await _serverService.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(ApiResponse.FailureResponse("Server not found."));
        if (!await _serverService.DeleteAsync(id, cancellationToken))
            return BadRequest(ApiResponse.FailureResponse("Cannot delete server while applications are linked to it."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Server deleted successfully."));
    }
}
