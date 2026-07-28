namespace tms_template_net8.Models.ViewModels;

public class DeployWatchOverviewViewModel
{
    public DateTime LastUpdated { get; set; }
    public string DateRangeLabel { get; set; } = string.Empty;
    public OverviewSummary Summary { get; set; } = new();
    public List<ApplicationRow> Applications { get; set; } = [];
    public List<ServerHealthItem> Servers { get; set; } = [];
    public List<RecentDeploymentItem> RecentDeployments { get; set; } = [];
}

public class OverviewSummary
{
    public int ApplicationsTotal { get; set; }
    public int ApplicationsHealthy { get; set; }
    public int ServersTotal { get; set; }
    public int ServersOnline { get; set; }
    public string AvgUptime { get; set; } = string.Empty;
    public int AlertsCount { get; set; }
}

public class ApplicationRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
    public string LastDeployment { get; set; } = string.Empty;
    public string DeployedBy { get; set; } = string.Empty;
}

public class ServerHealthItem
{
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public int HealthPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentDeploymentItem
{
    public string AppName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
