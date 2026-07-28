using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace tms_template_net8.Auth.Services;
/// <summary>
/// Calls the external auth API to obtain new tokens using a refresh token.
/// </summary>
public interface IAuthTokenRefreshService
{
    /// <summary>
    /// Returns new access (and optional refresh) tokens, or null if refresh failed or is not configured.
    /// </summary>
    Task<AuthRefreshedTokens?> TryRefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

/// <param name="AccessToken">New JWT access token.</param>
/// <param name="RefreshToken">New refresh token if the server rotated it; otherwise null to keep the existing cookie.</param>
public sealed record AuthRefreshedTokens(string AccessToken, string? RefreshToken);

public sealed class AuthTokenRefreshService : IAuthTokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthTokenRefreshService> _logger;
    private readonly IConfiguration _configuration;

    public AuthTokenRefreshService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthTokenRefreshService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthRefreshedTokens?> TryRefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var baseUrl = _configuration["Auth:BaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("Auth token refresh skipped: Auth:BaseUrl is not configured.");
            return null;
        }

        var url = baseUrl.TrimEnd('/') + "/api/auth/refresh-token";

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.ParseAdd("application/json");

            var bodyJson = BuildRequestBody(refreshToken);
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refresh token API returned {Status}: {Body}", (int)response.StatusCode, Truncate(payload, 500));
                return null;
            }

            var parsed = TryParseTokens(payload);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Value.AccessToken))
            {
                _logger.LogWarning("Refresh token API succeeded but response did not contain an access token.");
                return null;
            }

            return new AuthRefreshedTokens(parsed.Value.AccessToken.Trim(), parsed.Value.RefreshToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token request failed.");
            return null;
        }
    }

    private static string BuildRequestBody(string refreshToken) =>
        JsonSerializer.Serialize(new { refreshToken });

    private static (string AccessToken, string? RefreshToken)? TryParseTokens(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var access = FindTokenString(root, "accessToken", "access_token");
            var refresh = FindTokenString(root, "refreshToken", "refresh_token");

            if (!string.IsNullOrEmpty(access))
                return (access!, refresh);

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data))
            {
                access = FindTokenString(data, "accessToken", "access_token");
                refresh = FindTokenString(data, "refreshToken", "refresh_token");
                if (!string.IsNullOrEmpty(access))
                    return (access!, refresh);

                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out var inner))
                {
                    access = FindTokenString(inner, "accessToken", "access_token");
                    refresh = FindTokenString(inner, "refreshToken", "refresh_token");
                    if (!string.IsNullOrEmpty(access))
                        return (access!, refresh);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindTokenString(JsonElement obj, params string[] names)
    {
        if (obj.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var name in names)
        {
            if (!obj.TryGetProperty(name, out var prop))
                continue;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
        }

        return null;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;
        return s[..max] + "…";
    }
}
