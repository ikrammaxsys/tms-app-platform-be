namespace tms_template_net8.Models.DTOs.Organization;

public sealed class OrganizationItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
