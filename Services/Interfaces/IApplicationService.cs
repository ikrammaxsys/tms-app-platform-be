using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Services;
public interface IApplicationService
{
    Task<IReadOnlyList<ApplicationItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationItem?> CreateAsync(ApplicationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
