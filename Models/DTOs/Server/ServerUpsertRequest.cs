namespace tms_template_net8.Models.DTOs.Server;



public sealed class ServerUpsertRequest

{

    public string? IpAddress { get; set; }

    public string? Environment { get; set; }

    public string? InternalExternal { get; set; }

    public string? Country { get; set; }

    public string? Provider { get; set; }

    public string? Domain { get; set; }

    public int? OrganizationId { get; set; }

}

