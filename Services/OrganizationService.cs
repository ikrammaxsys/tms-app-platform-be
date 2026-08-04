using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Organization;

namespace tms_template_net8.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repository;
    private readonly IServerRepository _serverRepository;

    public OrganizationService(IOrganizationRepository repository, IServerRepository serverRepository)
    {
        _repository = repository;
        _serverRepository = serverRepository;
    }

    public Task<IReadOnlyList<OrganizationItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<OrganizationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<OrganizationItem?> CreateAsync(OrganizationUpsertRequest request, CancellationToken cancellationToken = default) =>
        await _repository.AddAsync(ToEntity(request), cancellationToken);

    public Task<bool> UpdateAsync(int id, OrganizationUpsertRequest request, CancellationToken cancellationToken = default) =>
        _repository.UpdateAsync(id, ToEntity(request), cancellationToken);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _serverRepository.AnyByOrganizationIdAsync(id, cancellationToken))
            return false;
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static OrganizationItem ToEntity(OrganizationUpsertRequest request) => new()
    {
        Code = (request.Code ?? string.Empty).Trim(),
        Name = (request.Name ?? string.Empty).Trim()
    };
}
