using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Data.Repositories;

public interface INagiosWindowServerRepository
{
    Task<IReadOnlyList<NagiosWindowServerItem>> GetLatestPerServerAsync(CancellationToken cancellationToken = default);
}
