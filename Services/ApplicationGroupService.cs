using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Services;
public sealed class ApplicationGroupService : IApplicationGroupService
{
    private readonly IApplicationGroupRepository _repository;
    private readonly IApplicationRepository _applicationRepository;
    public ApplicationGroupService(IApplicationGroupRepository repository, IApplicationRepository applicationRepository)
    {
        _repository = repository;
        _applicationRepository = applicationRepository;
    }

    public Task<IReadOnlyList<ApplicationGroupItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<ApplicationGroupItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<ApplicationGroupItem?> CreateAsync(ApplicationGroupUpsertRequest request, CancellationToken cancellationToken = default) =>
        await _repository.AddAsync(ToEntity(request), cancellationToken);

    public Task<bool> UpdateAsync(int id, ApplicationGroupUpsertRequest request, CancellationToken cancellationToken = default) =>
        _repository.UpdateAsync(id, ToEntity(request), cancellationToken);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // A group that still has applications assigned to it cannot be deleted.
        if (await _applicationRepository.AnyByGroupIdAsync(id, cancellationToken))
            return false;
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static ApplicationGroupItem ToEntity(ApplicationGroupUpsertRequest request) => new()
    {
        Name = (request.Name ?? string.Empty).Trim()
    };

    public async Task<IReadOnlyList<ApplicationItem>> GetApplicationsByApplicationGroupIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _applicationRepository.GetByGroupIdAsync(id, cancellationToken);
}
