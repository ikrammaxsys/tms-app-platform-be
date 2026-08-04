using tms_template_net8.Models.DTOs.Organization;

namespace tms_template_net8.Data.Repositories;

public interface IOrganizationRepository
{
    Task<IReadOnlyList<OrganizationItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationItem> AddAsync(OrganizationItem organization, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, OrganizationItem organization, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
