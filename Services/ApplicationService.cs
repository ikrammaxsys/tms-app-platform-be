using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Services;
public sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly IServerRepository _serverRepository;
    private readonly IApplicationGroupRepository _groupRepository;

    public ApplicationService(
        IApplicationRepository repository,
        IServerRepository serverRepository,
        IApplicationGroupRepository groupRepository)
    {
        _repository = repository;
        _serverRepository = serverRepository;
        _groupRepository = groupRepository;
    }

    public Task<IReadOnlyList<ApplicationItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<ApplicationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<ApplicationItem?> CreateAsync(ApplicationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (await _serverRepository.GetByIdAsync(request.ServerId, cancellationToken) is null)
            return null;
        if (await _groupRepository.GetByIdAsync(request.ApplicationGroupId, cancellationToken) is null)
            return null;
        return await _repository.AddAsync(ToEntity(request), cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, ApplicationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (await _serverRepository.GetByIdAsync(request.ServerId, cancellationToken) is null)
            return false;
        if (await _groupRepository.GetByIdAsync(request.ApplicationGroupId, cancellationToken) is null)
            return false;
        return await _repository.UpdateAsync(id, ToEntity(request), cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);
        
    private static ApplicationItem ToEntity(ApplicationUpsertRequest request) => new()
    {
        Name = (request.Name ?? string.Empty).Trim(),
        Version = (request.Version ?? string.Empty).Trim(),
        Commit = (request.Commit ?? string.Empty).Trim(),
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Healthy" : request.Status.Trim(),
        LastDeployment = request.LastDeployment,
        AppUrl = (request.AppUrl ?? string.Empty).Trim(),
        RepositoryUrl = (request.RepositoryUrl ?? string.Empty).Trim(),
        ServerId = request.ServerId,
        ApplicationGroupId = request.ApplicationGroupId
    };
}
