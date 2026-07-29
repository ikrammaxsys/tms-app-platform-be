using tms_template_net8.Common.Time;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Application;
using tms_template_net8.Models.DTOs.Uptime;

namespace tms_template_net8.Services;

public sealed class UptimeService : IUptimeService
{
    private static readonly HashSet<int> AllowedTimelineDays = [1, 7, 30];

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationDeploymentService _deploymentService;
    private readonly IApplicationUptimeLogRepository _uptimeLogs;

    public UptimeService(
        IApplicationRepository applicationRepository,
        IApplicationDeploymentService deploymentService,
        IApplicationUptimeLogRepository uptimeLogs)
    {
        _applicationRepository = applicationRepository;
        _deploymentService = deploymentService;
        _uptimeLogs = uptimeLogs;
    }

    public async Task<AgentUptimeReportResult?> ReportAsync(
        AgentUptimeReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var appUid = (request.AppId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(appUid))
            return null;

        var application = await _applicationRepository.GetByUidAsync(appUid, cancellationToken);
        if (application is null)
            return null;

        var now = MalaysiaTime.Now;
        var reportedVersion = (request.Version ?? string.Empty).Trim();
        var reportedCommit = (request.Commit ?? string.Empty).Trim();
        var previousVersion = application.Version ?? string.Empty;
        var isOnline = request.Status == 1;
        var versionDrift = isOnline
            && !string.Equals(previousVersion, reportedVersion, StringComparison.OrdinalIgnoreCase);

        int? deploymentId = null;
        if (versionDrift)
        {
            // Only apply version/commit updates when the agent reports Up (status=1).
            var deployment = await _deploymentService.CreateAsync(new ApplicationDeploymentUpsertRequest
            {
                ApplicationId = application.Id,
                Version = reportedVersion,
                CommitNo = reportedCommit,
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss")
            }, cancellationToken);

            if (deployment is null)
                throw new InvalidOperationException($"Failed to create deployment for application {application.Uid}.");

            deploymentId = deployment.Id;
        }

        var uptimeLog = await _uptimeLogs.AddAsync(new ApplicationUptimeLogItem
        {
            ApplicationId = application.Id,
            Status = MapAgentStatus(request.Status),
            Timestamp = now
        }, cancellationToken);

        return new AgentUptimeReportResult
        {
            ApplicationId = application.Id,
            ApplicationUid = application.Uid,
            VersionDrift = versionDrift,
            PreviousVersion = previousVersion,
            CurrentVersion = versionDrift ? reportedVersion : previousVersion,
            UptimeLogId = uptimeLog.Id,
            DeploymentId = deploymentId
        };
    }

    public async Task<UptimeTimelineResponse?> GetTimelineAsync(
        int applicationId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedTimelineDays.Contains(days))
            throw new ArgumentOutOfRangeException(nameof(days), "days must be 1, 7, or 30.");

        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        if (application is null)
            return null;

        var to = MalaysiaTime.Now;
        var from = days == 1
            ? to.Date
            : to.Date.AddDays(-(days - 1));

        var logs = await _uptimeLogs.GetByApplicationIdSinceAsync(applicationId, from, cancellationToken);
        var latest = await _uptimeLogs.GetLatestByApplicationIdAsync(applicationId, cancellationToken);
        var points = days == 1
            ? BuildHourlyPoints(from, logs)
            : BuildDailyPoints(from, days, logs);

        var upCount = logs.Count(IsUp);
        var degradedCount = logs.Count(IsDegraded);
        var downCount = logs.Count(IsDown);
        var total = logs.Count;
        var isOnline = latest is not null && IsUp(latest);

        return new UptimeTimelineResponse
        {
            ApplicationId = applicationId,
            IsOnline = isOnline,
            CurrentStatus = latest?.Status ?? "NoData",
            LastChecked = latest?.Timestamp,
            Days = days,
            Granularity = days == 1 ? "hour" : "day",
            From = from,
            To = to,
            UptimePercent = RoundPercent(total == 0 ? 100.0 : 100.0 * upCount / total),
            TotalChecks = total,
            UpCount = upCount,
            DegradedCount = degradedCount,
            DownCount = downCount,
            Points = points
        };
    }

    private static List<UptimeTimelinePoint> BuildHourlyPoints(
        DateTime from,
        IReadOnlyList<ApplicationUptimeLogItem> logs)
    {
        var byHour = logs
            .GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<UptimeTimelinePoint>(24);
        var endExclusive = from.AddDays(1);
        for (var hour = from; hour < endExclusive; hour = hour.AddHours(1))
        {
            byHour.TryGetValue(hour, out var hourLogs);
            points.Add(ToPoint(
                label: hour.ToString("HH:00"),
                from: hour,
                to: hour.AddHours(1),
                logs: hourLogs));
        }

        return points;
    }

    private static List<UptimeTimelinePoint> BuildDailyPoints(
        DateTime from,
        int days,
        IReadOnlyList<ApplicationUptimeLogItem> logs)
    {
        var byDay = logs
            .GroupBy(l => l.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<UptimeTimelinePoint>(days);
        for (var i = 0; i < days; i++)
        {
            var day = from.Date.AddDays(i);
            byDay.TryGetValue(day, out var dayLogs);
            points.Add(ToPoint(
                label: day.ToString("MMM d"),
                from: day,
                to: day.AddDays(1),
                logs: dayLogs));
        }

        return points;
    }

    private static UptimeTimelinePoint ToPoint(
        string label,
        DateTime from,
        DateTime to,
        List<ApplicationUptimeLogItem>? logs)
    {
        if (logs is null || logs.Count == 0)
        {
            return new UptimeTimelinePoint
            {
                Label = label,
                From = from,
                To = to,
                UptimePercent = null,
                Status = "NoData",
                TotalChecks = 0
            };
        }

        var upCount = logs.Count(IsUp);
        var degradedCount = logs.Count(IsDegraded);
        var downCount = logs.Count(IsDown);
        var status = downCount > 0 ? "Down" : degradedCount > 0 ? "Degraded" : "Up";

        return new UptimeTimelinePoint
        {
            Label = label,
            From = from,
            To = to,
            UptimePercent = RoundPercent(100.0 * upCount / logs.Count),
            Status = status,
            TotalChecks = logs.Count,
            UpCount = upCount,
            DegradedCount = degradedCount,
            DownCount = downCount
        };
    }

    private static bool IsUp(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Up", StringComparison.OrdinalIgnoreCase);

    private static bool IsDegraded(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Degraded", StringComparison.OrdinalIgnoreCase);

    private static bool IsDown(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Down", StringComparison.OrdinalIgnoreCase);

    private static double RoundPercent(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string MapAgentStatus(int? status) =>
        status == 0 ? "Down" : "Up";
}
