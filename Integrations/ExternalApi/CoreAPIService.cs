using System.Text.Json;
using tms_template_net8.Models.DTOs;

namespace tms_template_net8.Integrations.ExternalApi;

public sealed class CoreAPIService : ICoreAPIService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;

    public CoreAPIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/v1/status", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var payload = JsonSerializer.Deserialize<CoreApiResponse>(responseText, JsonOptions);

        return payload?.Data;
    }
}
