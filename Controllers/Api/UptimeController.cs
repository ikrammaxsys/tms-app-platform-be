using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Uptime;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/uptime")]
public class UptimeController : ControllerBase
{
    private readonly IUptimeService _uptimeService;

    public UptimeController(IUptimeService uptimeService)
    {
        _uptimeService = uptimeService;
    }

    /// <summary>
    /// Agent heartbeat endpoint. Records an uptime log and, on version drift,
    /// creates a deployment and updates the application version/commit.
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> Report(
        [FromBody] AgentUptimeReportRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return BadRequest(ApiResponse<AgentUptimeReportResult>.FailureResponse("Request body is required."));
        if (string.IsNullOrWhiteSpace(body.AppId))
            return BadRequest(ApiResponse<AgentUptimeReportResult>.FailureResponse("appId is required."));
        if (body.Status is not (0 or 1))
            return BadRequest(ApiResponse<AgentUptimeReportResult>.FailureResponse("status must be 0 (Down) or 1 (Up)."));

        var result = await _uptimeService.ReportAsync(body, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<AgentUptimeReportResult>.FailureResponse("Application not found for the given appId (uid)."));

        var message = result.VersionDrift
            ? "Version drift detected. Deployment created and uptime log stored."
            : "Uptime log stored successfully.";

        return Ok(ApiResponse<AgentUptimeReportResult>.SuccessResponse(result, message));
    }

    /// <summary>
    /// Aggregated uptime timeline for an application.
    /// days=1 → hourly buckets; days=7|30 → daily buckets.
    /// </summary>
    [HttpGet("{applicationId:int}/timeline")]
    public async Task<IActionResult> GetTimeline(
        int applicationId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        if (days is not (1 or 7 or 30))
            return BadRequest(ApiResponse<UptimeTimelineResponse>.FailureResponse("days must be 1, 7, or 30."));

        try
        {
            var result = await _uptimeService.GetTimelineAsync(applicationId, days, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<UptimeTimelineResponse>.FailureResponse("Application not found."));

            return Ok(ApiResponse<UptimeTimelineResponse>.SuccessResponse(
                result, $"Uptime timeline ({days} day) fetched successfully."));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<UptimeTimelineResponse>.FailureResponse(ex.Message));
        }
    }
}
