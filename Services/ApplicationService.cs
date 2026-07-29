using tms_template_net8.Common.Time;
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

        var entity = ToEntity(request);
        if (await _repository.UidExistsAsync(entity.Uid, null, cancellationToken))
            throw new InvalidOperationException($"Application uid '{entity.Uid}' already exists.");

        return await _repository.AddAsync(entity, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, ApplicationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (await _serverRepository.GetByIdAsync(request.ServerId, cancellationToken) is null)
            return false;
        if (await _groupRepository.GetByIdAsync(request.ApplicationGroupId, cancellationToken) is null)
            return false;

        var entity = ToEntity(request);
        if (await _repository.UidExistsAsync(entity.Uid, id, cancellationToken))
            throw new InvalidOperationException($"Application uid '{entity.Uid}' already exists.");

        return await _repository.UpdateAsync(id, entity, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);
        
    private static ApplicationItem ToEntity(ApplicationUpsertRequest request)
    {
        var uid = (request.Uid ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uid))
            uid = Guid.NewGuid().ToString("N");

        return new ApplicationItem
        {
            Uid = uid,
            Name = (request.Name ?? string.Empty).Trim(),
            Version = (request.Version ?? string.Empty).Trim(),
            Commit = (request.Commit ?? string.Empty).Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Healthy" : request.Status.Trim(),
            LastDeployment = request.LastDeployment.HasValue
                ? MalaysiaTime.ForStorage(request.LastDeployment)
                : null,
            AppUrl = (request.AppUrl ?? string.Empty).Trim(),
            RepositoryUrl = (request.RepositoryUrl ?? string.Empty).Trim(),
            ServerId = request.ServerId,
            ApplicationGroupId = request.ApplicationGroupId
        };
    }
}
