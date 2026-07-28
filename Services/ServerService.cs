using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Server;
namespace tms_template_net8.Services;
public sealed class ServerService : IServerService
{
    private readonly IServerRepository _repository;
    private readonly IApplicationRepository _applicationRepository;
    public ServerService(IServerRepository repository, IApplicationRepository applicationRepository)
    {
        _repository = repository;
        _applicationRepository = applicationRepository;
    }
    public Task<IReadOnlyList<ServerItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);
    public Task<ServerItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);
    public async Task<ServerItem?> CreateAsync(ServerUpsertRequest request, CancellationToken cancellationToken = default) =>
        await _repository.AddAsync(ToEntity(request), cancellationToken);
    public Task<bool> UpdateAsync(int id, ServerUpsertRequest request, CancellationToken cancellationToken = default) =>
        _repository.UpdateAsync(id, ToEntity(request), cancellationToken);
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _applicationRepository.AnyByServerIdAsync(id, cancellationToken))
            return false;
        return await _repository.DeleteAsync(id, cancellationToken);
    }
    private static ServerItem ToEntity(ServerUpsertRequest request) => new()
    {
        IpAddress = (request.IpAddress ?? string.Empty).Trim(),
        Environment = string.IsNullOrWhiteSpace(request.Environment) ? "Live" : request.Environment.Trim(),
        InternalExternal = string.IsNullOrWhiteSpace(request.InternalExternal) ? "Internal" : request.InternalExternal.Trim(),
        Country = (request.Country ?? string.Empty).Trim(),
        Provider = (request.Provider ?? string.Empty).Trim(),
        Domain = (request.Domain ?? string.Empty).Trim()
    };
}
