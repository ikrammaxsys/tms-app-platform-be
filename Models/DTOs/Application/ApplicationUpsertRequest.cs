namespace tms_template_net8.Models.DTOs.Application;
public sealed class ApplicationUpsertRequest
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Commit { get; set; }
    public string? Status { get; set; }
    public DateTime? LastDeployment { get; set; }
    public string? AppUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public int ServerId { get; set; }
    public int ApplicationGroupId { get; set; }
}
