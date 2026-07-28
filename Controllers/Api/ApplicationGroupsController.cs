using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Application;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Api;
[ApiController]
[Route("api/application-groups")]
public class ApplicationGroupsController : ControllerBase
{
    private readonly IApplicationGroupService _groupService;
    private readonly IServerService _serverService;

    public ApplicationGroupsController(IApplicationGroupService groupService, IServerService serverService)
    {
        _groupService = groupService;
        _serverService = serverService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _groupService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationGroupItem>>.SuccessResponse(items, "Application groups fetched successfully."));
    }

    // Dropdown options for the application create/edit forms.
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var options = (await _groupService.GetAllAsync(cancellationToken))
            .Select(x => new { value = x.Id.ToString(), text = x.Name })
            .ToList();
        return Ok(options);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _groupService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<ApplicationGroupItem>.FailureResponse("Application group not found."));
        return Ok(ApiResponse<ApplicationGroupItem>.SuccessResponse(item, "Application group fetched successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ApplicationGroupUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse<ApplicationGroupItem>.FailureResponse("Group name is required."));
        var created = await _groupService.CreateAsync(body, cancellationToken);
        if (created is null)
            return BadRequest(ApiResponse<ApplicationGroupItem>.FailureResponse("Failed to create application group."));
        return Ok(ApiResponse<ApplicationGroupItem>.SuccessResponse(created, "Application group created successfully."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ApplicationGroupUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse.FailureResponse("Group name is required."));
        if (!await _groupService.UpdateAsync(id, body, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Application group not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application group updated successfully."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await _groupService.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(ApiResponse.FailureResponse("Application group not found."));
        if (!await _groupService.DeleteAsync(id, cancellationToken))
            return BadRequest(ApiResponse.FailureResponse("Cannot delete group while applications are assigned to it."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application group deleted successfully."));
    }

    [HttpGet("{id:int}/applications")]
    public async Task<IActionResult> GetApplicationsByApplicationGroup(int id, CancellationToken cancellationToken)
    {
        var applications = await _groupService.GetApplicationsByApplicationGroupIdAsync(id, cancellationToken);

        foreach (var application in applications)
        {
            var server = await _serverService.GetByIdAsync(application.ServerId, cancellationToken);
            if (server is not null)
            {
                var serverDetail = new
                {
                    Domain = server.Domain,
                    Environment = server.Environment,
                    IpAddress = server.IpAddress,
                    InternalExternal = server.InternalExternal,
                    Country = server.Country,
                    Provider = server.Provider
                };
                application.ServerDetail = serverDetail;
            }
        }
        return Ok(ApiResponse<IReadOnlyList<ApplicationItem>>.SuccessResponse(applications, "Applications fetched successfully."));
    }
}
