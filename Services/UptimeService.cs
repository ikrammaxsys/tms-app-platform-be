using tms_template_net8.Common.Time;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Application;
using tms_template_net8.Models.DTOs.Server;
using tms_template_net8.Models.DTOs.Uptime;

namespace tms_template_net8.Services;

public sealed class UptimeService : IUptimeService
{
    private static readonly HashSet<int> AllowedTimelineDays = [1, 7, 30];
    private const int MaxTimelineMonths = 6;

    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationDeploymentService _deploymentService;
    private readonly IApplicationUptimeLogRepository _uptimeLogs;
    private readonly IServerRepository _servers;
    private readonly IServerMetricsRepository _serverMetrics;

    public UptimeService(
        IApplicationRepository applicationRepository,
        IApplicationDeploymentService deploymentService,
        IApplicationUptimeLogRepository uptimeLogs,
        IServerRepository servers,
        IServerMetricsRepository serverMetrics)
    {
        _applicationRepository = applicationRepository;
        _deploymentService = deploymentService;
        _uptimeLogs = uptimeLogs;
        _servers = servers;
        _serverMetrics = serverMetrics;
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

    public async Task<AgentHostReportResult?> ReportHostAsync(
        AgentHostReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var hostId = (request.HostId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(hostId))
            return null;

        var server = await _servers.GetByIpAddressAsync(hostId, cancellationToken);
        if (server is null)
            return null;

        var disks = request.Disks ?? [];
        var diskTotal = disks.Sum(d => d.TotalBytes ?? 0);
        var diskUsed = disks.Sum(d => d.UsedBytes ?? 0);
        var diskAvailable = disks.Sum(d => d.FreeBytes ?? 0);

        var timestamp = request.CollectedAt.HasValue
            ? MalaysiaTime.ForStorage(request.CollectedAt)
            : MalaysiaTime.Now;

        var metrics = await _serverMetrics.AddAsync(new ServerMetricsItem
        {
            ServerId = server.Id,
            CpuCores = request.CpuCores ?? 0,
            CpuUsage = RoundUsage(request.CpuUsagePercent ?? 0),
            RamTotal = request.MemoryTotalBytes ?? 0,
            RamUsage = request.MemoryUsedBytes ?? 0,
            RamAvailable = request.MemoryFreeBytes ?? 0,
            DiskTotal = diskTotal,
            DiskUsed = diskUsed,
            DiskAvailable = diskAvailable,
            Timestamp = timestamp
        }, cancellationToken);

        return new AgentHostReportResult
        {
            ServerId = server.Id,
            HostId = hostId,
            ServerMetricsId = metrics.Id,
            Timestamp = metrics.Timestamp
        };
    }

    public async Task<UptimeTimelineResponse?> GetTimelineAsync(
        int applicationId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedTimelineDays.Contains(days))
            throw new ArgumentOutOfRangeException(nameof(days), "days must be 1, 7, or 30.");

        var to = MalaysiaTime.Now;
        var from = days == 1
            ? to.Date
            : to.Date.AddDays(-(days - 1));
        var toExclusive = to.Date.AddDays(1);

        return await BuildApplicationTimelineAsync(
            applicationId,
            from,
            to,
            toExclusive,
            days,
            days == 1 ? "hour" : "day",
            cancellationToken);
    }

    public async Task<UptimeTimelineResponse?> GetTimelineByDateRangeAsync(
        int applicationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var from = startDate.Date;
        var toExclusive = endDate.Date.AddDays(1);
        ValidateTimelineDateRange(from, toExclusive);

        var now = MalaysiaTime.Now;
        var to = endDate.Date >= now.Date ? now : endDate.Date.AddDays(1).AddTicks(-1);
        var days = (toExclusive - from).Days;

        return await BuildApplicationTimelineAsync(
            applicationId,
            from,
            to,
            toExclusive,
            days,
            days == 1 ? "hour" : "day",
            cancellationToken);
    }

    private async Task<UptimeTimelineResponse?> BuildApplicationTimelineAsync(
        int applicationId,
        DateTime from,
        DateTime to,
        DateTime toExclusive,
        int days,
        string granularity,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken);
        if (application is null)
            return null;

        var logs = await _uptimeLogs.GetByApplicationIdBetweenAsync(applicationId, from, toExclusive, cancellationToken);
        var latest = await _uptimeLogs.GetLatestByApplicationIdAsync(applicationId, cancellationToken);
        var now = MalaysiaTime.Now;
        var points = granularity == "hour"
            ? BuildHourlyPoints(from, logs, now)
            : BuildDailyPoints(from, days, logs, now);

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
            Granularity = granularity,
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

    private static void ValidateTimelineDateRange(DateTime from, DateTime toExclusive)
    {
        if (from >= toExclusive)
            throw new ArgumentOutOfRangeException(nameof(from), "startDate must be before or equal to endDate.");

        if (from.AddMonths(MaxTimelineMonths) < toExclusive)
            throw new ArgumentOutOfRangeException(
                nameof(toExclusive),
                $"Date range cannot exceed {MaxTimelineMonths} months.");

        var today = MalaysiaTime.Now.Date;
        if (toExclusive.AddDays(-1) > today)
            throw new ArgumentOutOfRangeException(nameof(toExclusive), "endDate cannot be in the future.");
    }

    public async Task<HostMetricsTimelineResponse?> GetHostMetricsTimelineAsync(
        int serverId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedTimelineDays.Contains(days))
            throw new ArgumentOutOfRangeException(nameof(days), "days must be 1, 7, or 30.");

        var server = await _servers.GetByIdAsync(serverId, cancellationToken);
        if (server is null)
            return null;

        var to = MalaysiaTime.Now;
        var from = days == 1
            ? to.Date
            : to.Date.AddDays(-(days - 1));

        var metrics = await _serverMetrics.GetByServerIdSinceAsync(serverId, from, cancellationToken);
        var latest = await _serverMetrics.GetLatestByServerIdAsync(serverId, cancellationToken);
        var points = days == 1
            ? BuildHostHourlyPoints(from, metrics)
            : BuildHostDailyPoints(from, days, metrics);

        var upCount = metrics.Count(IsHostUp);
        var degradedCount = metrics.Count(IsHostDegraded);
        var downCount = metrics.Count(IsHostDown);
        var total = metrics.Count;
        var isOnline = latest is not null && IsHostUp(latest);

        return new HostMetricsTimelineResponse
        {
            ServerId = serverId,
            IsOnline = isOnline,
            CurrentStatus = latest is null ? "NoData" : MapHostStatus(latest),
            LastChecked = latest?.Timestamp,
            CurrentCpuUsage = latest?.CpuUsage,
            CurrentRam = latest is null ? null : ToRamMetrics(latest),
            CurrentDisk = latest is null ? null : ToDiskMetrics(latest),
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

    private static List<HostMetricsTimelinePoint> BuildHostHourlyPoints(
        DateTime from,
        IReadOnlyList<ServerMetricsItem> metrics)
    {
        var byHour = metrics
            .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<HostMetricsTimelinePoint>(24);
        var endExclusive = from.AddDays(1);
        for (var hour = from; hour < endExclusive; hour = hour.AddHours(1))
        {
            byHour.TryGetValue(hour, out var hourMetrics);
            points.Add(ToHostPoint(
                label: hour.ToString("HH:00"),
                from: hour,
                to: hour.AddHours(1),
                metrics: hourMetrics));
        }

        return points;
    }

    private static List<HostMetricsTimelinePoint> BuildHostDailyPoints(
        DateTime from,
        int days,
        IReadOnlyList<ServerMetricsItem> metrics)
    {
        var byDay = metrics
            .GroupBy(m => m.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<HostMetricsTimelinePoint>(days);
        for (var i = 0; i < days; i++)
        {
            var day = from.Date.AddDays(i);
            byDay.TryGetValue(day, out var dayMetrics);
            points.Add(ToHostPoint(
                label: day.ToString("MMM d"),
                from: day,
                to: day.AddDays(1),
                metrics: dayMetrics));
        }

        return points;
    }

    private static HostMetricsTimelinePoint ToHostPoint(
        string label,
        DateTime from,
        DateTime to,
        List<ServerMetricsItem>? metrics)
    {
        if (metrics is null || metrics.Count == 0)
        {
            return new HostMetricsTimelinePoint
            {
                Label = label,
                From = from,
                To = to,
                UptimePercent = null,
                Status = "NoData",
                TotalChecks = 0
            };
        }

        var upCount = metrics.Count(IsHostUp);
        var degradedCount = metrics.Count(IsHostDegraded);
        var downCount = metrics.Count(IsHostDown);
        var status = downCount > 0 ? "Down" : degradedCount > 0 ? "Degraded" : "Up";

        return new HostMetricsTimelinePoint
        {
            Label = label,
            From = from,
            To = to,
            UptimePercent = RoundPercent(100.0 * upCount / metrics.Count),
            Status = status,
            TotalChecks = metrics.Count,
            UpCount = upCount,
            DegradedCount = degradedCount,
            DownCount = downCount,
            AvgCpuUsage = RoundUsage((double)metrics.Average(m => m.CpuUsage)),
            Ram = AggregateRamMetrics(metrics),
            Disk = AggregateDiskMetrics(metrics)
        };
    }

    private static HostResourceMetrics ToRamMetrics(ServerMetricsItem metric) =>
        ToResourceMetrics(metric.RamTotal, metric.RamUsage, metric.RamAvailable);

    private static HostResourceMetrics ToDiskMetrics(ServerMetricsItem metric) =>
        ToResourceMetrics(metric.DiskTotal, metric.DiskUsed, metric.DiskAvailable);

    private static HostResourceMetrics AggregateRamMetrics(IReadOnlyList<ServerMetricsItem> metrics)
    {
        var total = (long)metrics.Average(m => m.RamTotal);
        var used = (long)metrics.Average(m => m.RamUsage);
        var available = (long)metrics.Average(m => m.RamAvailable);
        return ToResourceMetrics(total, used, available);
    }

    private static HostResourceMetrics AggregateDiskMetrics(IReadOnlyList<ServerMetricsItem> metrics)
    {
        var total = (long)metrics.Average(m => m.DiskTotal);
        var used = (long)metrics.Average(m => m.DiskUsed);
        var available = (long)metrics.Average(m => m.DiskAvailable);
        return ToResourceMetrics(total, used, available);
    }

    private static HostResourceMetrics ToResourceMetrics(long total, long used, long available) =>
        new()
        {
            TotalBytes = total,
            UsedBytes = used,
            AvailableBytes = available,
            UsagePercent = total > 0 ? RoundPercent(100.0 * used / total) : 0
        };

    private static bool IsHostUp(ServerMetricsItem metric) =>
        MapHostStatus(metric).Equals("Up", StringComparison.OrdinalIgnoreCase);

    private static bool IsHostDegraded(ServerMetricsItem metric) =>
        MapHostStatus(metric).Equals("Degraded", StringComparison.OrdinalIgnoreCase);

    private static bool IsHostDown(ServerMetricsItem metric) =>
        MapHostStatus(metric).Equals("Down", StringComparison.OrdinalIgnoreCase);

    private static string MapHostStatus(ServerMetricsItem metric)
    {
        var cpu = (double)metric.CpuUsage;
        var ram = RamUsagePercent(metric);
        var disk = DiskUsagePercent(metric);

        if (cpu >= 90 || ram >= 95 || disk >= 95)
            return "Down";
        if (cpu >= 75 || ram >= 85 || disk >= 85)
            return "Degraded";
        return "Up";
    }

    private static double RamUsagePercent(ServerMetricsItem metric) =>
        metric.RamTotal > 0 ? 100.0 * metric.RamUsage / metric.RamTotal : 0;

    private static double DiskUsagePercent(ServerMetricsItem metric) =>
        metric.DiskTotal > 0 ? 100.0 * metric.DiskUsed / metric.DiskTotal : 0;

    private static List<UptimeTimelinePoint> BuildHourlyPoints(
        DateTime from,
        IReadOnlyList<ApplicationUptimeLogItem> logs,
        DateTime now)
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
                logs: hourLogs,
                now));
        }

        return points;
    }

    private static List<UptimeTimelinePoint> BuildDailyPoints(
        DateTime from,
        int days,
        IReadOnlyList<ApplicationUptimeLogItem> logs,
        DateTime now)
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
                logs: dayLogs,
                now));
        }

        return points;
    }

    private static UptimeTimelinePoint ToPoint(
        string label,
        DateTime from,
        DateTime to,
        List<ApplicationUptimeLogItem>? logs,
        DateTime now)
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
        var status = ResolveBucketStatus(logs, from, to, now, upCount, degradedCount, downCount);

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

    /// <summary>
    /// In-progress buckets use the latest check (matches currentStatus).
    /// Completed buckets summarize the full period instead of marking Down on any single failure.
    /// </summary>
    private static string ResolveBucketStatus(
        IReadOnlyList<ApplicationUptimeLogItem> logs,
        DateTime from,
        DateTime to,
        DateTime now,
        int upCount,
        int degradedCount,
        int downCount)
    {
        if (now >= from && now < to)
        {
            var latest = logs.OrderByDescending(l => l.Timestamp).First();
            return latest.Status;
        }

        if (downCount == logs.Count)
            return "Down";
        if (upCount == logs.Count)
            return "Up";
        if (degradedCount > 0 && downCount == 0)
            return "Degraded";
        if (downCount > 0 && upCount == 0)
            return "Down";

        var uptimeRatio = (double)upCount / logs.Count;
        return uptimeRatio >= 0.95 ? "Degraded" : downCount >= upCount ? "Down" : "Degraded";
    }

    private static bool IsUp(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Up", StringComparison.OrdinalIgnoreCase);

    private static bool IsDegraded(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Degraded", StringComparison.OrdinalIgnoreCase);

    private static bool IsDown(ApplicationUptimeLogItem log) =>
        log.Status.Equals("Down", StringComparison.OrdinalIgnoreCase);

    private static double RoundPercent(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundUsage(double value) =>
        Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);

    private static string MapAgentStatus(int? status) =>
        status == 0 ? "Down" : "Up";
}
