using Microsoft.AspNetCore.Http;
using tms_template_net8.Auth.Services;
namespace tms_template_net8.Middleware;

/// <summary>
/// Requires a valid JWT cookie and a completed ACL check for protected app routes.
/// If the access token is expired but a refresh token cookie is present, attempts to refresh via the auth API and updates cookies before continuing.
/// </summary>
public sealed class AccessTokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public AccessTokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITokenService tokenService,
        IAuthTokenRefreshService refreshService)
    {
        if (ShouldSkipTokenCheck(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Cookies["authacl_access_token"];

        if (string.IsNullOrWhiteSpace(token))
        {
            await RejectUnauthorizedAsync(context);
            return;
        }

        var (principal, kind) = tokenService.ValidateTokenWithKind(token);

        if (kind == AuthTokenValidationKind.Valid && principal != null)
        {
            context.User = principal;
            if (!await HasCompletedAclCheckAsync(context))
            {
                await RejectUnauthorizedAsync(context);
                return;
            }

            await _next(context);
            return;
        }

        if (kind == AuthTokenValidationKind.Expired)
        {
            var refreshToken = context.Request.Cookies["authacl_refresh_token"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var refreshed = await refreshService.TryRefreshAsync(refreshToken.Trim(), context.RequestAborted);
                if (refreshed != null)
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        Path = "/",
                        SameSite = SameSiteMode.Lax,
                    };
                    context.Response.Cookies.Append("authacl_access_token", refreshed.AccessToken, cookieOptions);
                    if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
                        context.Response.Cookies.Append("authacl_refresh_token", refreshed.RefreshToken!, cookieOptions);

                    var (newPrincipal, newKind) = tokenService.ValidateTokenWithKind(refreshed.AccessToken);
                    if (newKind == AuthTokenValidationKind.Valid && newPrincipal != null)
                    {
                        context.User = newPrincipal;
                        if (!await HasCompletedAclCheckAsync(context))
                        {
                            await RejectUnauthorizedAsync(context);
                            return;
                        }

                        await _next(context);
                        return;
                    }
                }
            }
        }

        await RejectUnauthorizedAsync(context);
    }

    private static bool ShouldSkipTokenCheck(PathString path)
    {
        // Root is the verify entry when default route is ACLChecking; external apps redirect here without a cookie yet.
        if (!path.HasValue || path == "/")
            return true;

        return path.StartsWithSegments("/ACLChecking", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/Home/SessionExpired", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/Home/AccessDenied", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/Login", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> HasCompletedAclCheckAsync(HttpContext context)
    {
        await context.Session.LoadAsync(context.RequestAborted);
        return string.Equals(context.Session.GetString("AclCheckPassed"), "1", StringComparison.Ordinal);
    }

    private static async Task RejectUnauthorizedAsync(HttpContext context)
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Authentication or ACL verification is required."
            });
            return;
        }

        var basePath = context.Request.PathBase.HasValue
            ? context.Request.PathBase.ToString().TrimEnd('/')
            : string.Empty;
        context.Response.Redirect(basePath + "/Home/SessionExpired");
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Headers.Accept.ToString(), "application/json", StringComparison.OrdinalIgnoreCase);
    }
}
