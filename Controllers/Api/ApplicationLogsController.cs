using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.ApplicationLogs;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/application-logs")]
public class ApplicationLogsController : ControllerBase
{
    private readonly IApplicationLogService _applicationLogService;

    public ApplicationLogsController(IApplicationLogService applicationLogService)
    {
        _applicationLogService = applicationLogService;
    }

    /// <summary>
    /// Agent endpoint. Accepts log JSON, compresses it to .gz, uploads to remote file storage,
    /// and stores metadata in application_logs / application_log_chunks.
    /// </summary>
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(
        [FromBody] AgentApplicationLogRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return BadRequest(ApiResponse<AgentApplicationLogResult>.FailureResponse("Request body is required."));
        if (string.IsNullOrWhiteSpace(body.AppUid))
            return BadRequest(ApiResponse<AgentApplicationLogResult>.FailureResponse("appUid is required."));
        if (string.IsNullOrWhiteSpace(body.Date))
            return BadRequest(ApiResponse<AgentApplicationLogResult>.FailureResponse("date is required."));
        if (!ApplicationLogPayloadHelper.IsValidLogJson(body.LogJson))
            return BadRequest(ApiResponse<AgentApplicationLogResult>.FailureResponse("log_json is required."));

        try
        {
            var result = await _applicationLogService.IngestAsync(body, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<AgentApplicationLogResult>.FailureResponse(
                    "Application not found for the given appUid."));

            return Ok(ApiResponse<AgentApplicationLogResult>.SuccessResponse(
                result, "Application log ingested and uploaded successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AgentApplicationLogResult>.FailureResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<AgentApplicationLogResult>.FailureResponse(ex.Message));
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<AgentApplicationLogResult>.FailureResponse(
                    "Failed to reach Core API file storage.", [ex.Message]));
        }
    }

    /// <summary>
    /// Lists all log dates and chunk metadata for an application.
    /// </summary>
    [HttpGet("{applicationId:int}/list")]
    public async Task<IActionResult> List(
        int applicationId,
        CancellationToken cancellationToken)
    {
        var result = await _applicationLogService.GetLogListAsync(applicationId, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<ApplicationLogListResponse>.FailureResponse("Application not found."));

        return Ok(ApiResponse<ApplicationLogListResponse>.SuccessResponse(
            result, "Application log list fetched successfully."));
    }

    /// <summary>
    /// Retrieves a specific log chunk for a given date. Pass chunk to fetch that exact chunk;
    /// omit chunk to return the first chunk for the date.
    /// </summary>
    [HttpGet("{applicationId:int}")]
    public async Task<IActionResult> GetChunk(
        int applicationId,
        [FromQuery] string? date,
        [FromQuery] string? chunk,
        [FromQuery(Name = "current_chunk")] string? currentChunk,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(date))
            return BadRequest(ApiResponse<ApplicationLogChunkContentResponse>.FailureResponse("date is required."));

        var requestedChunk = string.IsNullOrWhiteSpace(chunk) ? currentChunk : chunk;

        try
        {
            var result = await _applicationLogService.GetChunkAsync(
                applicationId, date, requestedChunk, cancellationToken);
            if (result is null)
                return NotFound(ApiResponse<ApplicationLogChunkContentResponse>.FailureResponse(
                    "Application, log date, or requested chunk not found."));

            return Ok(ApiResponse<ApplicationLogChunkContentResponse>.SuccessResponse(
                result, "Application log chunk fetched successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ApplicationLogChunkContentResponse>.FailureResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<ApplicationLogChunkContentResponse>.FailureResponse(ex.Message));
        }
        catch (JsonException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<ApplicationLogChunkContentResponse>.FailureResponse(
                    $"Failed to parse log chunk content: {ex.Message}"));
        }
    }
}
