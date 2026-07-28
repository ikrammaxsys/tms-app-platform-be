using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using tms_template_net8.Auth.Models;
namespace tms_template_net8.Auth.Services;

/// <summary>
/// Loads the user's roles + per-resource access controls from the auth API,
/// caches the snapshot in <c>HttpContext.Session</c>, and exposes a
/// <c>HasAccess(name, right)</c> check used by the page-access authorization filter.
/// </summary>
public interface IUserAccessControlService
{
    /// <summary>
    /// Calls the configured ACL endpoint, parses the standard
    /// <c>{ success, data: { user, roles, accessControls } }</c> envelope, and stores the result in session.
    /// Returns the loaded snapshot, or <c>null</c> if the call failed (the failure is non-fatal so
    /// the caller can still surface a friendly error).
    /// </summary>
    Task<UserAclData?> LoadAndStoreAsync(HttpContext context, string idAclUser, string? bearerToken, CancellationToken cancellationToken = default);

    /// <summary>Reads the cached snapshot from the current session, or <c>null</c> when not present.</summary>
    UserAclData? GetCurrent(HttpContext context);

    /// <summary>Convenience wrapper around <see cref="UserAclData.HasAccess"/> that returns <c>false</c> when no snapshot is cached.</summary>
    bool HasAccess(HttpContext context, string accessName, AccessRight right);

    /// <summary>Removes the cached snapshot from session (used on logout).</summary>
    void Clear(HttpContext context);
}

public sealed class UserAccessControlService : IUserAccessControlService
{
    public const string DefaultSessionKey = "UserAclData";

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserAccessControlService> _logger;
    private readonly IConfiguration _configuration;

    public UserAccessControlService(
        IHttpClientFactory httpClientFactory,
        ILogger<UserAccessControlService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserAclData?> LoadAndStoreAsync(HttpContext context, string idAclUser, string? bearerToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(idAclUser))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (!string.IsNullOrWhiteSpace(bearerToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

            var url = BuildRolesAndAccessUrl(idAclUser);
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ACL endpoint {Url} returned {Status}.", url, (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<AclApiResponse>(payload, SerializeOptions)?.Data;
            if (data == null)
                return null;

            await context.Session.LoadAsync(cancellationToken).ConfigureAwait(false);
            context.Session.SetString(DefaultSessionKey, JsonSerializer.Serialize(data));
            await context.Session.CommitAsync(cancellationToken).ConfigureAwait(false);

            return data;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user roles and access for idAclUser={IdAclUser}.", idAclUser);
            return null;
        }
    }

    public UserAclData? GetCurrent(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var raw = context.Session.GetString(DefaultSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UserAclData>(raw, SerializeOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached UserAclData from session; treating as empty.");
            return null;
        }
    }

    public bool HasAccess(HttpContext context, string accessName, AccessRight right)
    {
        var snapshot = GetCurrent(context);
        return snapshot != null && snapshot.HasAccess(accessName, right);
    }

    public void Clear(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Session.Remove(DefaultSessionKey);
    }

    private string BuildRolesAndAccessUrl(string idAclUser)
    {
        var path = $"/api/users/{Uri.EscapeDataString(idAclUser)}/role-access";
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        var baseUrl = _configuration["Dsp:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        return baseUrl + path;
    }
    private sealed class AclApiResponse
    {
        public UserAclData? Data { get; set; }
    }
}
