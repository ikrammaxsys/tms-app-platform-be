using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Services;
public interface IApplicationGroupService
{
    Task<IReadOnlyList<ApplicationGroupItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationGroupItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationGroupItem?> CreateAsync(ApplicationGroupUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationGroupUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationItem>> GetApplicationsByApplicationGroupIdAsync(int id, CancellationToken cancellationToken = default);
}
