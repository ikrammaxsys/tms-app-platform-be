namespace tms_template_net8.Models.ViewModels;

public class ApplicationDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public int ApplicationGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public bool HasVersionDrift { get; set; }

    public HealthOverview Health { get; set; } = new();
    public DeploymentSummary CurrentDeployment { get; set; } = new();
    public List<DeploymentHistoryItem> DeploymentHistory { get; set; } = [];
    public List<AppServerRow> Servers { get; set; } = [];
    public List<VersionDriftGroup> VersionDrift { get; set; } = [];
    public List<HourlyHealthSegment> TodayTimeline { get; set; } = [];
    public List<DailyAvailabilityDay> AvailabilityDays { get; set; } = [];
    public PerformanceSnapshot Performance { get; set; } = new();
    public List<AppEventItem> RecentEvents { get; set; } = [];
    public List<DependencyItem> Dependencies { get; set; } = [];
    public List<ConfigItem> EnvironmentVariables { get; set; } = [];
    public List<EndpointItem> Endpoints { get; set; } = [];
    public AlertSummary Alerts { get; set; } = new();
    public List<LogEntry> Logs { get; set; } = [];
}

public class HealthOverview
{
    public string Status { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public double AvailabilityPercent { get; set; }
    public string CurrentUptime { get; set; } = string.Empty;
    public string LastRestart { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Memory { get; set; } = string.Empty;
    public string Disk { get; set; } = string.Empty;
}

public class DeploymentSummary
{
    public string Version { get; set; } = string.Empty;
    public string Released { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string BuiltBy { get; set; } = string.Empty;
    public string DeployedBy { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

public class DeploymentHistoryItem
{
    public string Version { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string By { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class AppServerRow
{
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public bool IsOutdated { get; set; }
}

public class VersionDriftGroup
{
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int ServerCount { get; set; }
    public bool IsCurrent { get; set; }
}

public class HourlyHealthSegment
{
    public string HourLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class DailyAvailabilityDay
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Healthy";
    public string Label => Date.ToString("MMM d");
}

public class PerformanceSnapshot
{
    public string RequestsPerSec { get; set; } = string.Empty;
    public string AvgResponseMs { get; set; } = string.Empty;
    public string ErrorRate { get; set; } = string.Empty;
    public string P95LatencyMs { get; set; } = string.Empty;
    public int[] CpuSparkline { get; set; } = [];
    public int[] MemorySparkline { get; set; } = [];
    public int[] RequestsSparkline { get; set; } = [];
}

public class AppEventItem
{
    public string When { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

public class DependencyItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ConfigItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Masked { get; set; }
}

public class EndpointItem
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string LastChecked { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
}

public class AlertSummary
{
    public int Warnings { get; set; }
    public int Critical { get; set; }
    public int Restarts { get; set; }
    public int Deployments { get; set; }
}

public class LogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
}
