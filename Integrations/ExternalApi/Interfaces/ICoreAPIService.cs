namespace tms_template_net8.Integrations.ExternalApi;

public interface ICoreAPIService
{
    /// <summary>
    /// Calls Core API GET /api/v1/status and returns the response data message.
    /// </summary>
    Task<string?> GetStatusAsync(CancellationToken cancellationToken = default);
}
