using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Application;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Api;
[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _applicationService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationItem>>.SuccessResponse(items, "Applications fetched successfully."));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var rows = (await _applicationService.GetAllAsync(cancellationToken))
            .Select(x => new
            {
                x.Id,
                x.Uid,
                x.Name,
                x.Version,
                x.Commit,
                x.Status,
                LastDeployment = x.LastDeployment?.ToString("yyyy-MM-dd HH:mm") ?? "",
                x.AppUrl,
                x.ApplicationGroupName,
                x.ServerDomain,
                x.ServerEnvironment
            })
            .ToList();
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _applicationService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<ApplicationItem>.FailureResponse("Application not found."));
        return Ok(ApiResponse<ApplicationItem>.SuccessResponse(item, "Application fetched successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ApplicationUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse<ApplicationItem>.FailureResponse("Application name is required."));
        if (string.IsNullOrWhiteSpace(body.Version))
            return BadRequest(ApiResponse<ApplicationItem>.FailureResponse("Version is required."));
        if (body.ServerId <= 0)
            return BadRequest(ApiResponse<ApplicationItem>.FailureResponse("Server is required."));
        if (body.ApplicationGroupId <= 0)
            return BadRequest(ApiResponse<ApplicationItem>.FailureResponse("Application group is required."));
        try
        {
            var created = await _applicationService.CreateAsync(body, cancellationToken);
            if (created is null)
                return BadRequest(ApiResponse<ApplicationItem>.FailureResponse("Server or application group not found."));
            return Ok(ApiResponse<ApplicationItem>.SuccessResponse(created, "Application created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApplicationItem>.FailureResponse(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ApplicationUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse.FailureResponse("Application name is required."));
        if (string.IsNullOrWhiteSpace(body.Version))
            return BadRequest(ApiResponse.FailureResponse("Version is required."));
        if (string.IsNullOrWhiteSpace(body.Uid))
            return BadRequest(ApiResponse.FailureResponse("UID is required."));
        if (body.ServerId <= 0)
            return BadRequest(ApiResponse.FailureResponse("Server is required."));
        if (body.ApplicationGroupId <= 0)
            return BadRequest(ApiResponse.FailureResponse("Application group is required."));
        try
        {
            if (!await _applicationService.UpdateAsync(id, body, cancellationToken))
                return NotFound(ApiResponse.FailureResponse("Application, server, or group not found."));
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Application updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.FailureResponse(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _applicationService.DeleteAsync(id, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Application not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application deleted successfully."));
    }
}
