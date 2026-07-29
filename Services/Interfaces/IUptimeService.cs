using tms_template_net8.Models.DTOs.Uptime;

namespace tms_template_net8.Services;

public interface IUptimeService
{
    Task<AgentUptimeReportResult?> ReportAsync(AgentUptimeReportRequest request, CancellationToken cancellationToken = default);
    Task<UptimeTimelineResponse?> GetTimelineAsync(int applicationId, int days, CancellationToken cancellationToken = default);
}
