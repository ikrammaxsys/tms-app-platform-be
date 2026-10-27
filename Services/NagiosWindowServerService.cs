using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Nagios;

namespace tms_template_net8.Services;

public sealed class NagiosWindowServerService : INagiosWindowServerService
{
    private readonly INagiosWindowServerRepository _repository;

    public NagiosWindowServerService(INagiosWindowServerRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<NagiosWindowServerItem>> GetLatestPerServerAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetLatestPerServerAsync(cancellationToken);
}
