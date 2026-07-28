using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using tms_template_net8.Auth.Models;

namespace tms_template_net8.Auth.Services;

public sealed record AclIndexResult(string TokenKey, string? Error, string? RedirectPathAndQuery);

public sealed record AclVerifyResult(
    bool Success,
    string? Message,
    string RedirectUrl,
    string UserId = "");

public sealed record AclLogoutPageData(string TokenKey, string DspBaseUrl);

public interface IAclCheckingService
{
    Task<AclIndexResult> ProcessIndexAsync(HttpContext context, string? authCode, CancellationToken cancellationToken = default);
    Task<AclVerifyResult> VerifyAsync(HttpContext context, AclTokenRequest? body, CancellationToken cancellationToken = default);
    string GetDspRedirectUrl(HttpContext context);
    AclLogoutPageData GetLogoutPageData(HttpContext context);
    void Logout(HttpContext context);
}

public sealed class AclCheckingService : IAclCheckingService
{
    public const string AclSessionKey = "AclCheckPassed";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly IUserAccessControlService _userAccessControlService;

    public AclCheckingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITokenService tokenService,
        IUserAccessControlService userAccessControlService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _tokenService = tokenService;
        _userAccessControlService = userAccessControlService;
    }

    public async Task<AclIndexResult> ProcessIndexAsync(HttpContext context, string? authCode, CancellationToken cancellationToken = default)
    {
        var tokenKey = "authacl_access_token";
        var refreshKey = "authacl_refresh_token";
        var cookieOptions = CreateAuthCookieOptions(context.Request);

        if (string.IsNullOrWhiteSpace(authCode))
            return new AclIndexResult(tokenKey, null, null);

        var exchanged = await ExchangeAuthCodeAsync(authCode, cancellationToken).ConfigureAwait(false);
        if (!exchanged.Success)
            return new AclIndexResult(tokenKey, exchanged.Message ?? "Auth code exchange failed.", null);

        ReplaceCookie(context.Response.Cookies, tokenKey, exchanged.AccessToken!, cookieOptions);
        ReplaceCookie(context.Response.Cookies, refreshKey, exchanged.RefreshToken!, cookieOptions);

        var redirectPath = context.Request.PathBase + context.Request.Path
            + BuildRedirectQueryWithout(context.Request, "auth-code");
        return new AclIndexResult(tokenKey, null, redirectPath);
    }

    public async Task<AclVerifyResult> VerifyAsync(HttpContext context, AclTokenRequest? body, CancellationToken cancellationToken = default)
    {
        await context.Session.LoadAsync(cancellationToken).ConfigureAwait(false);

        var dspRedirectUrl = GetDspRedirectUrl(context);
        var tokenKey = "authacl_access_token";

        var token = body?.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            token = context.Request.Cookies[tokenKey]?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return new AclVerifyResult(false, "Access token is missing.", dspRedirectUrl);

        var (_, tokenKind) = _tokenService.ValidateTokenWithKind(token);
        if (tokenKind != AuthTokenValidationKind.Valid)
        {
            var message = tokenKind == AuthTokenValidationKind.Expired ? "token_expired" : "invalid_token";
            return new AclVerifyResult(false, message, dspRedirectUrl);
        }

        var idAclFromBody = body?.IdAclUser?.Trim();
        if (!string.IsNullOrEmpty(idAclFromBody))
            context.Session.SetString("ID_ACL_USER", idAclFromBody);

        var sessionUserId = context.Session.GetString("ID_ACL_USER")?.Trim() ?? string.Empty;

        UserAclData? aclData = null;
        if (!string.IsNullOrEmpty(sessionUserId))
            aclData = await _userAccessControlService.LoadAndStoreAsync(context, sessionUserId, token, cancellationToken).ConfigureAwait(false);

        if (!HasAppAccess(aclData, sessionUserId))
        {
            _userAccessControlService.Clear(context);
            return new AclVerifyResult(false, "You do not have access to this application.", dspRedirectUrl);
        }

        context.Session.SetString(AclSessionKey, "1");
        context.Session.SetString("gstrUserID", sessionUserId);
        if (!string.IsNullOrEmpty(sessionUserId))
            context.Session.SetString("gstrUserName", sessionUserId);

        var homeUrl = BuildHomeUrl(context.Request);
        return new AclVerifyResult(true, null, homeUrl, sessionUserId);
    }

    public string GetDspRedirectUrl(HttpContext context)
    {
        var dspBaseUrl = _configuration["Dsp:BaseUrl"]?.Trim();
        if (string.IsNullOrEmpty(dspBaseUrl))
            dspBaseUrl = "/";

        var loginPath = _configuration["Dsp:LoginPath"]?.Trim();
        if (string.IsNullOrEmpty(loginPath))
            loginPath = "/Auth/Login";

        return BuildDspRedirectUrl(context.Request, dspBaseUrl, loginPath);
    }

    public AclLogoutPageData GetLogoutPageData(HttpContext context)
    {
        return new AclLogoutPageData("authacl_access_token", GetDspRedirectUrl(context));
    }

    public void Logout(HttpContext context)
    {
        context.Session.Clear();

        var cookieOptions = CreateAuthCookieOptions(context.Request);
        context.Response.Cookies.Delete("authacl_access_token", cookieOptions);
        context.Response.Cookies.Delete("authacl_refresh_token", cookieOptions);
        context.Response.Cookies.Delete(".AspNetCore.Session");
    }

    private async Task<(bool Success, string? Message, string? AccessToken, string? RefreshToken)> ExchangeAuthCodeAsync(
        string authCode,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Auth:BaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
            return (false, "Auth service URL is not configured.", null, null);

        var url = baseUrl.TrimEnd('/') + "/api/auth/exchange-auth-code";
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.ParseAdd("application/json");
            var bodyJson = JsonSerializer.Serialize(new { authCode });
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return (false, TryParseApiErrorMessage(payload) ?? "Auth code exchange failed.", null, null);

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.TryGetProperty("success", out var successNode) && successNode.ValueKind == JsonValueKind.False)
            {
                var msg = TryParseApiErrorMessage(payload);
                return (false, msg ?? "Auth code exchange was not successful.", null, null);
            }

            var access = root.GetProperty("data").GetProperty("accessToken").GetString();
            var refresh = root.GetProperty("data").TryGetProperty("refreshToken", out var rt) && rt.ValueKind == JsonValueKind.String
                ? rt.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(access))
                return (false, "Exchange response did not contain an access token.", null, null);

            return (true, null, access.Trim(), string.IsNullOrWhiteSpace(refresh) ? null : refresh.Trim());
        }
        catch
        {
            return (false, "Auth code exchange error.", null, null);
        }
    }

    private bool HasAppAccess(UserAclData? aclData, string sessionUserId)
    {
        if (string.IsNullOrEmpty(sessionUserId) || aclData == null)
            return false;

        var appName = _configuration["AppName"]?.Trim();
        if (string.IsNullOrEmpty(appName))
            return false;

        return aclData.HasAccess(appName, AccessRight.View);
    }

    private static string BuildHomeUrl(HttpRequest request)
    {
        var basePath = request.PathBase.HasValue
            ? request.PathBase.ToString().TrimEnd('/')
            : string.Empty;
        return string.IsNullOrEmpty(basePath) ? "/Home/Index" : basePath + "/Home/Index";
    }

    private static string BuildAppReturnUrl(HttpRequest request)
    {
        var basePath = request.PathBase.HasValue
            ? request.PathBase.ToString().TrimEnd('/')
            : string.Empty;
        var path = string.IsNullOrEmpty(basePath) ? "/ACLChecking" : basePath + "/ACLChecking";
        return $"{request.Scheme}://{request.Host}{path}";
    }

    private static string BuildDspRedirectUrl(HttpRequest request, string dspBaseUrl, string loginPath)
    {
        var returnUrl = BuildAppReturnUrl(request);
        if (!loginPath.StartsWith('/'))
            loginPath = "/" + loginPath;

        var target = dspBaseUrl.TrimEnd('/') + loginPath;
        var separator = target.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{target}{separator}returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static CookieOptions CreateAuthCookieOptions(HttpRequest request)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            Path = "/",
            SameSite = SameSiteMode.Lax,
        };
    }

    private static void ReplaceCookie(IResponseCookies cookies, string name, string value, CookieOptions options)
    {
        cookies.Delete(name, options);
        cookies.Append(name, value, options);
    }

    private static string BuildRedirectQueryWithout(HttpRequest request, params string[] excludedKeys)
    {
        var qb = new QueryBuilder();
        foreach (var kv in request.Query)
        {
            if (excludedKeys.Any(k => string.Equals(kv.Key, k, StringComparison.OrdinalIgnoreCase)))
                continue;
            foreach (var v in kv.Value)
                qb.Add(kv.Key, v ?? string.Empty);
        }
        return qb.ToQueryString().ToString();
    }

    private static string? TryParseApiErrorMessage(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString();
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
            {
                var first = errs[0];
                if (first.ValueKind == JsonValueKind.String)
                    return first.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
