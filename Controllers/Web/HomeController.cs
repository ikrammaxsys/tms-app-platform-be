using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Auth.Services;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.ViewModels;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Web;
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAclCheckingService _aclCheckingService;
    private readonly IApplicationService _applicationService;
    private readonly IServerService _serverService;
    private readonly IApplicationUptimeLogRepository _uptimeLogs;
    public HomeController(
        ILogger<HomeController> logger,
        IAclCheckingService aclCheckingService,
        IApplicationService applicationService,
        IServerService serverService,
        IApplicationUptimeLogRepository uptimeLogs)
    {
        _logger = logger;
        _aclCheckingService = aclCheckingService;
        _applicationService = applicationService;
        _serverService = serverService;
        _uptimeLogs = uptimeLogs;
    }
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View("~/Views/Home/Index.cshtml", await BuildOverviewAsync(cancellationToken));
    }
    public async Task<IActionResult> Application(int id, CancellationToken cancellationToken)
    {
        var model = await BuildApplicationDetailAsync(id, cancellationToken);
        if (model == null)
            return NotFound();
        return View(model);
    }
    public IActionResult Roadmap()
    {
        return View(BuildRoadmap());
    }
    private static RoadmapViewModel BuildRoadmap() => new()
    {
        Ideas =
        [
            new() { Title = "Centralized logs viewer", Description = "Search and filter app logs across all servers in one place.", Tag = "Observability" },
            new() { Title = "New Deployment Notification", Description = "Send notification to team when new deployment is deployed.", Tag = "Notification" },
        ],
        InProgress =
        [
            new() { Title = "Application Monitoring", Description = "Monitor application version and deployment status.", Tag = "Monitoring" },
            new() { Title = "Server Tracking", Description = "Track server for all encironment.", Tag = "Tracking" },
            new() { Title = "Availability Monitoring", Description = "Monitor applications availability and uptime.", Tag = "Monitoring" },
            new() { Title = "App Platform Agent", Description = "Agent to monitor application performance and latency in background.", Tag = "Monitoring" },
        ],
        Shipped =
        [
          
        ]
    };
    private async Task<DeployWatchOverviewViewModel> BuildOverviewAsync(CancellationToken cancellationToken)
    {
        var apps = await _applicationService.GetAllAsync(cancellationToken);
        var servers = await _serverService.GetAllAsync(cancellationToken);
        var colors = new[] { "#16a34a", "#2563eb", "#0891b2", "#db2777", "#7c3aed", "#ca8a04", "#0d9488", "#ea580c" };
        var rows = new List<ApplicationRow>();
        foreach (var a in apps)
        {
            var avail = await BuildAvailabilityDaysAsync(a.Id, a.Status, cancellationToken);
            var upPct = avail.Count == 0
                ? 100.0
                : 100.0 * avail.Count(d => d.Status is "Healthy" or "Operational") / avail.Count;
            rows.Add(new ApplicationRow
            {
                Id = a.Id,
                Name = a.Name,
                Type = string.IsNullOrWhiteSpace(a.ApplicationGroupName) ? "Application" : a.ApplicationGroupName,
                Initial = string.IsNullOrEmpty(a.Name) ? "?" : a.Name[..1].ToUpperInvariant(),
                AvatarColor = colors[Math.Abs(a.Name.GetHashCode()) % colors.Length],
                Server = a.ServerDomain,
                Environment = a.ServerEnvironment,
                Version = a.Version,
                Status = a.Status,
                Uptime = $"{upPct:0.00}%",
                LastDeployment = a.LastDeployment?.ToString("MMM dd, yyyy hh:mm tt") ?? "-",
                DeployedBy = a.Commit
            });
        }
        var warningServers = apps
            .GroupBy(a => a.ServerId)
            .Where(g => g.Any(x => x.Status is "Warning" or "Down"))
            .Select(g => g.Key)
            .ToHashSet();
        return new DeployWatchOverviewViewModel
        {
            LastUpdated = DateTime.Now,
            DateRangeLabel = $"{DateTime.Today:MMM dd, yyyy} - {DateTime.Today:MMM dd, yyyy}",
            Summary = new OverviewSummary
            {
                ApplicationsTotal = apps.Count,
                ApplicationsHealthy = apps.Count(a => a.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase)),
                ServersTotal = servers.Count,
                ServersOnline = servers.Count - warningServers.Count,
                AvgUptime = rows.Count == 0
                    ? "100.00%"
                    : $"{rows.Average(r => double.Parse(r.Uptime.TrimEnd('%'), CultureInfo.InvariantCulture)):0.00}%",
                AlertsCount = apps.Count(a => a.Status is "Warning" or "Down")
            },
            Applications = rows,
            Servers = servers.Select(s => new ServerHealthItem
            {
                Name = s.Domain,
                Environment = s.Environment,
                HealthPercent = warningServers.Contains(s.Id) ? 78 : 99,
                Status = warningServers.Contains(s.Id) ? "Warning" : "Healthy"
            }).ToList(),
            RecentDeployments = apps
                .Where(a => a.LastDeployment.HasValue)
                .OrderByDescending(a => a.LastDeployment)
                .Take(5)
                .Select(a => new RecentDeploymentItem
                {
                    AppName = $"{a.Name} ({a.ServerEnvironment})",
                    Version = a.Version,
                    Timestamp = a.LastDeployment!.Value.ToString("MMM dd, yyyy hh:mm tt"),
                    Status = a.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase) ? "Success" : a.Status
                })
                .ToList()
        };
    }
    private async Task<ApplicationDetailViewModel?> BuildApplicationDetailAsync(int id, CancellationToken cancellationToken)
    {
        var app = await _applicationService.GetByIdAsync(id, cancellationToken);
        if (app is null) return null;
        var server = await _serverService.GetByIdAsync(app.ServerId, cancellationToken);
        var isDown = app.Status.Equals("Down", StringComparison.OrdinalIgnoreCase);
        var isWarning = app.Status.Equals("Warning", StringComparison.OrdinalIgnoreCase);
        var availabilityDays = await BuildAvailabilityDaysAsync(app.Id, app.Status, cancellationToken);
        var healthyDays = availabilityDays.Count(d => d.Status is "Healthy");
        var availPct = availabilityDays.Count == 0 ? 100.0 : 100.0 * healthyDays / availabilityDays.Count;
        var sameName = (await _applicationService.GetAllAsync(cancellationToken))
            .Where(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var hasDrift = sameName.Select(a => a.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
        var colors = new[] { "#16a34a", "#2563eb", "#0891b2", "#db2777", "#7c3aed", "#ca8a04", "#0d9488", "#ea580c" };
        var color = colors[Math.Abs(app.Name.GetHashCode()) % colors.Length];
        var avgLatency = await AverageLatencyAsync(app.Id, cancellationToken);
        var todayTimeline = await BuildTodayTimelineFromLogsAsync(app.Id, isDown, isWarning, cancellationToken);
        return new ApplicationDetailViewModel
        {
            Id = app.Id.ToString(),
            ApplicationGroupId = app.ApplicationGroupId,
            Name = app.Name,
            Type = app.ApplicationGroupName,
            Initial = app.Name[..1].ToUpperInvariant(),
            AvatarColor = color,
            Status = app.Status,
            Version = app.Version,
            Environment = app.ServerEnvironment,
            Owner = server?.Provider ?? "-",
            Repository = string.IsNullOrWhiteSpace(app.RepositoryUrl) ? app.Commit : app.RepositoryUrl,
            RepositoryUrl = string.IsNullOrWhiteSpace(app.RepositoryUrl) ? "#" : app.RepositoryUrl,
            Runtime = server?.InternalExternal ?? "-",
            HasVersionDrift = hasDrift,
            Health = new HealthOverview
            {
                Status = app.Status,
                Availability = $"{availPct:0.00}%",
                AvailabilityPercent = availPct,
                CurrentUptime = isDown ? "0m" : $"{healthyDays}d (30d window)",
                LastRestart = app.LastDeployment?.ToString("dd MMM yyyy") ?? "-",
                Cpu = isDown ? "0%" : isWarning ? "68%" : "31%",
                Memory = isDown ? "0 GB / 8 GB" : isWarning ? "5.2 GB / 8 GB" : "2.4 GB / 8 GB",
                Disk = "52%"
            },
            CurrentDeployment = new DeploymentSummary
            {
                Version = app.Version,
                Released = app.LastDeployment?.ToString("dd MMM yyyy") ?? "-",
                Commit = app.Commit,
                Branch = "-",
                BuiltBy = "-",
                DeployedBy = "-",
                Duration = "-"
            },
            Servers =
            [
                new AppServerRow
                {
                    Name = app.ServerDomain,
                    Environment = app.ServerEnvironment,
                    Version = app.Version,
                    Uptime = $"{availPct:0.0}%",
                    Status = app.Status,
                    StatusLabel = app.Status,
                    IsOutdated = false
                }
            ],
            VersionDrift = sameName
                .GroupBy(a => a.ServerEnvironment)
                .Select(g => new VersionDriftGroup
                {
                    Environment = g.Key,
                    Version = g.First().Version,
                    ServerCount = g.Count(),
                    IsCurrent = g.Any(x => x.Id == app.Id)
                })
                .ToList(),
            TodayTimeline = todayTimeline,
            AvailabilityDays = availabilityDays,
            Performance = new PerformanceSnapshot
            {
                RequestsPerSec = isDown ? "0" : "42",
                AvgResponseMs = avgLatency?.ToString() ?? "-",
                ErrorRate = isDown ? "100%" : isWarning ? "2.4%" : "0.08%",
                P95LatencyMs = isDown ? "-" : "148",
                CpuSparkline = [12, 18, 16, 22, 28, 35, 30, 24, 20, 18, 22, 28],
                MemorySparkline = [40, 42, 45, 48, 52, 55, 58, 56, 50, 48, 46, 44],
                RequestsSparkline = [20, 28, 35, 40, 38, 45, 50, 42, 36, 30, 34, 42]
            },
            Endpoints =
            [
                new()
                {
                    Name = "App URL",
                    Method = "GET",
                    Path = string.IsNullOrWhiteSpace(app.AppUrl) ? $"{app.ServerDomain}" : app.AppUrl,
                    StatusCode = isDown ? "Down" : "OK",
                    LastChecked = "from uptime logs",
                    ActionLabel = "Open"
                },
                new()
                {
                    Name = "Host",
                    Method = "TCP",
                    Path = $"{app.ServerDomain} / {app.ServerIpAddress}",
                    StatusCode = isDown ? "Down" : "OK",
                    LastChecked = "from uptime logs"
                }
            ],
            Alerts = new AlertSummary
            {
                Warnings = isWarning || isDown ? 2 : 0,
                Critical = isDown ? 1 : 0,
                Restarts = 0,
                Deployments = 1
            }
        };
    }
    private async Task<List<DailyAvailabilityDay>> BuildAvailabilityDaysAsync(
        int applicationId,
        string appStatus,
        CancellationToken cancellationToken)
    {
        var since = DateTime.Today.AddDays(-29);
        var logs = await _uptimeLogs.GetByApplicationIdSinceAsync(applicationId, since, cancellationToken);
        var byDay = logs
            .GroupBy(l => l.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.ToList());
        var days = new List<DailyAvailabilityDay>(30);
        for (var i = 29; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var status = "Healthy";
            if (byDay.TryGetValue(date, out var dayLogs))
            {
                if (dayLogs.Any(l => l.Status.Equals("Down", StringComparison.OrdinalIgnoreCase)))
                    status = "Down";
                else if (dayLogs.Any(l => l.Status.Equals("Degraded", StringComparison.OrdinalIgnoreCase)))
                    status = "Partial";
            }
            else if (appStatus.Equals("Down", StringComparison.OrdinalIgnoreCase) && i <= 2)
            {
                status = "Down";
            }
            days.Add(new DailyAvailabilityDay { Date = date, Status = status });
        }
        return days;
    }
    private async Task<List<HourlyHealthSegment>> BuildTodayTimelineFromLogsAsync(
        int applicationId,
        bool isDown,
        bool isWarning,
        CancellationToken cancellationToken)
    {
        var hours = new[] { "00", "02", "04", "06", "08", "10", "12", "14", "16", "18", "20", "22" };
        var todayLogs = await _uptimeLogs.GetByApplicationIdSinceAsync(applicationId, DateTime.Today, cancellationToken);
        var hasDownToday = todayLogs.Any(l => l.Status.Equals("Down", StringComparison.OrdinalIgnoreCase));
        return hours.Select((h, i) =>
        {
            var status = "Healthy";
            if (isDown && i >= 8) status = "Down";
            else if ((isWarning || hasDownToday) && (i == 4 || i == 5)) status = "Down";
            return new HourlyHealthSegment { HourLabel = h, Status = status };
        }).ToList();
    }
    private async Task<int?> AverageLatencyAsync(int applicationId, CancellationToken cancellationToken)
    {
        var logs = await _uptimeLogs.GetByApplicationIdSinceAsync(applicationId, DateTime.Today.AddDays(-29), cancellationToken);
        var values = logs.Where(l => l.Latency.HasValue).Select(l => l.Latency!.Value).ToList();
        return values.Count == 0 ? null : (int)values.Average();
    }
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public IActionResult SessionExpired()
    {
        ViewBag.DspRedirectUrl = _aclCheckingService.GetDspRedirectUrl(HttpContext);
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult AccessDenied(string? name = null, string? right = null)
    {
        ViewBag.AccessName = name;
        ViewBag.AccessRight = right;
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
