using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Services;

public interface INagiosWindowServerService
{
    Task<IReadOnlyList<NagiosWindowServerItem>> GetLatestPerServerAsync(CancellationToken cancellationToken = default);
}
