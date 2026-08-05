using tms_template_net8.Models.DTOs.Uptime;

namespace tms_template_net8.Services;

public interface IUptimeService
{
    Task<AgentUptimeReportResult?> ReportAsync(AgentUptimeReportRequest request, CancellationToken cancellationToken = default);
    Task<AgentHostReportResult?> ReportHostAsync(AgentHostReportRequest request, CancellationToken cancellationToken = default);
    Task<UptimeTimelineResponse?> GetTimelineAsync(int applicationId, int days, CancellationToken cancellationToken = default);
    Task<UptimeTimelineResponse?> GetTimelineByDateRangeAsync(
        int applicationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    Task<HostMetricsTimelineResponse?> GetHostMetricsTimelineAsync(int serverId, int days, CancellationToken cancellationToken = default);
}
