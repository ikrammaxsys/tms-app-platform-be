using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Nagios;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/nagios")]
public class NagiosController : ControllerBase
{
    private readonly INagiosWindowServerService _nagiosWindowServerService;

    public NagiosController(INagiosWindowServerService nagiosWindowServerService)
    {
        _nagiosWindowServerService = nagiosWindowServerService;
    }

    [HttpGet("window-server/statistics")]
    public async Task<IActionResult> GetWindowServerStatistics(CancellationToken cancellationToken)
    {
        var items = await _nagiosWindowServerService.GetLatestPerServerAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NagiosWindowServerItem>>.SuccessResponse(
            items, "Window server statistics fetched successfully."));
    }
}
