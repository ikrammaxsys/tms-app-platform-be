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
    /// Host agent metrics endpoint. Records CPU, memory, and disk usage for a registered server.
    /// </summary>
    [HttpPost("report-host")]
    public async Task<IActionResult> ReportHost(
        [FromBody] AgentHostReportRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return BadRequest(ApiResponse<AgentHostReportResult>.FailureResponse("Request body is required."));
        if (string.IsNullOrWhiteSpace(body.HostId))
            return BadRequest(ApiResponse<AgentHostReportResult>.FailureResponse("hostId is required."));

        var result = await _uptimeService.ReportHostAsync(body, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<AgentHostReportResult>.FailureResponse(
                "Server not found for the given hostId (ip_address)."));

        return Ok(ApiResponse<AgentHostReportResult>.SuccessResponse(result, "Server metrics stored successfully."));
    }

    /// <summary>
    /// Aggregated uptime timeline for an application.
    /// Use days=1|7|30, or pass startDate/endDate (max 6 months, yyyy-MM-dd).
    /// Single-day ranges use hourly buckets; longer ranges use daily buckets.
    /// </summary>
    [HttpGet("{applicationId:int}/timeline")]
    public async Task<IActionResult> GetTimeline(
        int applicationId,
        [FromQuery] int? days,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            UptimeTimelineResponse? result;
            string message;

            if (startDate.HasValue || endDate.HasValue)
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest(ApiResponse<UptimeTimelineResponse>.FailureResponse(
                        "Both startDate and endDate are required when using a custom date range."));
                }

                result = await _uptimeService.GetTimelineByDateRangeAsync(
                    applicationId, startDate.Value, endDate.Value, cancellationToken);
                message = $"Uptime timeline ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}) fetched successfully.";
            }
            else
            {
                var timelineDays = days ?? 7;
                if (timelineDays is not (1 or 7 or 30))
                    return BadRequest(ApiResponse<UptimeTimelineResponse>.FailureResponse("days must be 1, 7, or 30."));

                result = await _uptimeService.GetTimelineAsync(applicationId, timelineDays, cancellationToken);
                message = $"Uptime timeline ({timelineDays} day) fetched successfully.";
            }

            if (result is null)
                return NotFound(ApiResponse<UptimeTimelineResponse>.FailureResponse("Application not found."));

            return Ok(ApiResponse<UptimeTimelineResponse>.SuccessResponse(result, message));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<UptimeTimelineResponse>.FailureResponse(ex.Message));
        }
    }

    /// <summary>
    /// Aggregated host metrics timeline for a server.
    /// days=1 → hourly buckets; days=7|30 → daily buckets.
    /// </summary>
    [HttpGet("{serverId:int}/host-timeline")]
    public async Task<IActionResult> GetHostTimeline(
        int serverId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        if (days is not (1 or 7 or 30))
            return BadRequest(ApiResponse<HostMetricsTimelineResponse>.FailureResponse("days must be 1, 7, or 30."));

        try
        {
            var result = await _uptimeService.GetHostMetricsTimelineAsync(serverId, days, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<HostMetricsTimelineResponse>.FailureResponse("Server not found."));

            return Ok(ApiResponse<HostMetricsTimelineResponse>.SuccessResponse(
                result, $"Host metrics timeline ({days} day) fetched successfully."));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<HostMetricsTimelineResponse>.FailureResponse(ex.Message));
        }
    }
}
