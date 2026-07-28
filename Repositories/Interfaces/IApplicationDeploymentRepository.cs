using tms_template_net8.Models.DTOs.Application;

namespace tms_template_net8.Data.Repositories;

public interface IApplicationDeploymentRepository
{
    Task<IReadOnlyList<ApplicationDeploymentItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationDeploymentItem>> GetByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationDeploymentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationDeploymentItem> AddAsync(ApplicationDeploymentItem deployment, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationDeploymentItem deployment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default);
}
