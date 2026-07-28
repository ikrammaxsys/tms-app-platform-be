using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Application;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/application-deployments")]
public class ApplicationDeploymentsController : ControllerBase
{
    private readonly IApplicationDeploymentService _deploymentService;

    public ApplicationDeploymentsController(IApplicationDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? applicationId, CancellationToken cancellationToken)
    {
        if (applicationId is > 0)
        {
            var byApp = await _deploymentService.GetByApplicationIdAsync(applicationId.Value, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<ApplicationDeploymentItem>>.SuccessResponse(
                byApp, "Application deployments fetched successfully."));
        }

        var items = await _deploymentService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationDeploymentItem>>.SuccessResponse(
            items, "Application deployments fetched successfully."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _deploymentService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<ApplicationDeploymentItem>.FailureResponse("Application deployment not found."));
        return Ok(ApiResponse<ApplicationDeploymentItem>.SuccessResponse(item, "Application deployment fetched successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ApplicationDeploymentUpsertRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return BadRequest(ApiResponse<ApplicationDeploymentItem>.FailureResponse("Request body is required."));
        if (body.ApplicationId <= 0)
            return BadRequest(ApiResponse<ApplicationDeploymentItem>.FailureResponse("Application is required."));
        if (string.IsNullOrWhiteSpace(body.Version))
            return BadRequest(ApiResponse<ApplicationDeploymentItem>.FailureResponse("Version is required."));

        var created = await _deploymentService.CreateAsync(body, cancellationToken);
        if (created is null)
            return BadRequest(ApiResponse<ApplicationDeploymentItem>.FailureResponse("Application not found."));
        return Ok(ApiResponse<ApplicationDeploymentItem>.SuccessResponse(created, "Application deployment created successfully."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ApplicationDeploymentUpsertRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return BadRequest(ApiResponse.FailureResponse("Request body is required."));
        if (body.ApplicationId <= 0)
            return BadRequest(ApiResponse.FailureResponse("Application is required."));
        if (string.IsNullOrWhiteSpace(body.Version))
            return BadRequest(ApiResponse.FailureResponse("Version is required."));

        if (!await _deploymentService.UpdateAsync(id, body, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Application deployment or application not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application deployment updated successfully."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _deploymentService.DeleteAsync(id, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Application deployment not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application deployment deleted successfully."));
    }
}
