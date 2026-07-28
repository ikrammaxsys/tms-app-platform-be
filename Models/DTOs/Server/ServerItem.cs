namespace tms_template_net8.Models.DTOs.Server;



public sealed class ServerItem

{

    public int Id { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string Environment { get; set; } = "Live";

    public string InternalExternal { get; set; } = "Internal";

    public string Country { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

}

