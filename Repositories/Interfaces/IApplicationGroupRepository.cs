using tms_template_net8.Models.DTOs.Application;
namespace tms_template_net8.Data.Repositories;
public interface IApplicationGroupRepository
{
    Task<IReadOnlyList<ApplicationGroupItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationGroupItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApplicationGroupItem> AddAsync(ApplicationGroupItem group, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ApplicationGroupItem group, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
