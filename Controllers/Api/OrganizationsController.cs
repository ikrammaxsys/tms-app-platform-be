using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Organization;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _organizationService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrganizationItem>>.SuccessResponse(items, "Organizations fetched successfully."));
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var options = (await _organizationService.GetAllAsync(cancellationToken))
            .Select(x => new { value = x.Id.ToString(), text = $"{x.Code} - {x.Name}" })
            .ToList();
        return Ok(options);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _organizationService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(ApiResponse<OrganizationItem>.FailureResponse("Organization not found."));
        return Ok(ApiResponse<OrganizationItem>.SuccessResponse(item, "Organization fetched successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrganizationUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Code))
            return BadRequest(ApiResponse<OrganizationItem>.FailureResponse("Organization code is required."));
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse<OrganizationItem>.FailureResponse("Organization name is required."));
        var created = await _organizationService.CreateAsync(body, cancellationToken);
        if (created is null)
            return BadRequest(ApiResponse<OrganizationItem>.FailureResponse("Failed to create organization."));
        return Ok(ApiResponse<OrganizationItem>.SuccessResponse(created, "Organization created successfully."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrganizationUpsertRequest? body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Code))
            return BadRequest(ApiResponse.FailureResponse("Organization code is required."));
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse.FailureResponse("Organization name is required."));
        if (!await _organizationService.UpdateAsync(id, body, cancellationToken))
            return NotFound(ApiResponse.FailureResponse("Organization not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Organization updated successfully."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await _organizationService.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(ApiResponse.FailureResponse("Organization not found."));
        if (!await _organizationService.DeleteAsync(id, cancellationToken))
            return BadRequest(ApiResponse.FailureResponse("Cannot delete organization while servers are linked to it."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Organization deleted successfully."));
    }
}
