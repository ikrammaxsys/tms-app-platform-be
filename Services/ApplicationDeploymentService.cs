using tms_template_net8.Common.Time;
using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Application;

namespace tms_template_net8.Services;

public sealed class ApplicationDeploymentService : IApplicationDeploymentService
{
    private readonly IApplicationDeploymentRepository _repository;
    private readonly IApplicationRepository _applicationRepository;

    public ApplicationDeploymentService(
        IApplicationDeploymentRepository repository,
        IApplicationRepository applicationRepository)
    {
        _repository = repository;
        _applicationRepository = applicationRepository;
    }

    public Task<IReadOnlyList<ApplicationDeploymentItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<ApplicationDeploymentItem>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default) =>
        _repository.GetByApplicationIdAsync(applicationId, cancellationToken);

    public Task<ApplicationDeploymentItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<ApplicationDeploymentItem?> CreateAsync(
        ApplicationDeploymentUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ApplicationId <= 0)
            return null;

        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return null;

        var entity = ToEntity(request);
        var created = await _repository.AddAsync(entity, cancellationToken);

        // Keep the application row in sync with the latest deployment (version/commit/last_deployment).
        var lastDeployment = TryParseTimestamp(entity.Timestamp) ?? MalaysiaTime.Now;
        var updated = await _applicationRepository.UpdateCurrentDeploymentAsync(
            application.Id,
            entity.Version,
            string.IsNullOrWhiteSpace(entity.CommitNo) ? application.Commit : entity.CommitNo,
            lastDeployment,
            cancellationToken);

        if (!updated)
            throw new InvalidOperationException(
                $"Deployment created but failed to update application {application.Id} version.");

        return created;
    }

    public async Task<bool> UpdateAsync(
        int id,
        ApplicationDeploymentUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ApplicationId <= 0)
            return false;
        if (await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken) is null)
            return false;
        return await _repository.UpdateAsync(id, ToEntity(request), cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    private static ApplicationDeploymentItem ToEntity(ApplicationDeploymentUpsertRequest request) => new()
    {
        ApplicationId = request.ApplicationId,
        CommitNo = (request.CommitNo ?? string.Empty).Trim(),
        Version = (request.Version ?? string.Empty).Trim(),
        Timestamp = MalaysiaTime.ForStorageString(request.Timestamp)
    };

    private static DateTime? TryParseTimestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return null;
        return DateTime.TryParse(timestamp, out var parsed) ? parsed : null;
    }
}
