using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public interface IApplicationRepository
{
    Task<IReadOnlyList<ApplicationItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationItem> AddAsync(ApplicationItem application, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationItem application, CancellationToken cancellationToken = default);
    Task<bool> UpdateCurrentDeploymentAsync(int id, string version, string commit, DateTime? lastDeployment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AnyByServerIdAsync(int serverId, CancellationToken cancellationToken = default);
    Task<bool> AnyByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationItem>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
}
