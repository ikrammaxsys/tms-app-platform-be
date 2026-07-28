using System.Net;
using System.Net.Http.Headers;

namespace tms_template_net8.Integrations.ExternalApi;

public sealed class ACLService : IACLService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ACLService> _logger;

    public ACLService(HttpClient httpClient, ILogger<ACLService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetUserByIdAsync(string id, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Uri.EscapeDataString(id)}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetSidebar(string idAclUser, string systemName, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idAclUser) || string.IsNullOrWhiteSpace(systemName))
            return null;

        var query = $"idAclUser={Uri.EscapeDataString(idAclUser)}&systemName={Uri.EscapeDataString(systemName)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/acl/sidebar?{query}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
