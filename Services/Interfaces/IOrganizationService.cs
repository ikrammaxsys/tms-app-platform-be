using tms_template_net8.Models.DTOs.Organization;

namespace tms_template_net8.Services;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationItem?> CreateAsync(OrganizationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, OrganizationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
