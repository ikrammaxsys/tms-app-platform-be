namespace tms_template_net8.Models.DTOs.Application;
public sealed class ApplicationItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy";
    public DateTime? LastDeployment { get; set; }
    public string AppUrl { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public int ServerId { get; set; }
    public int ApplicationGroupId { get; set; }
    // Joined display fields
    public string ServerDomain { get; set; } = string.Empty;
    public string ServerEnvironment { get; set; } = string.Empty;
    public string ServerIpAddress { get; set; } = string.Empty;
    public string ApplicationGroupName { get; set; } = string.Empty;
    public dynamic? ServerDetail { get; set; }
}
