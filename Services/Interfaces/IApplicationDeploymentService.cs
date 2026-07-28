using tms_template_net8.Models.DTOs.Application;

namespace tms_template_net8.Services;

public interface IApplicationDeploymentService
{
    Task<IReadOnlyList<ApplicationDeploymentItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationDeploymentItem>> GetByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationDeploymentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationDeploymentItem?> CreateAsync(ApplicationDeploymentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationDeploymentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
