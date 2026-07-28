using Microsoft.AspNetCore.Builder;
using tms_template_net8.Auth.Security;
using tms_template_net8.Auth.Services;
using tms_template_net8.Middleware;

namespace tms_template_net8.Auth;

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registers everything the ACL gate + access-token middleware needs:
    /// RSA signing key, JWT validator, refresh-token client, and the ACL session cache.
    /// </summary>
    public static IServiceCollection AddAuthAndAcl(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var rsa = RsaKeyLoader.LoadRsaKey(config, env);
        services.AddSingleton(rsa);

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthTokenRefreshService, AuthTokenRefreshService>();
        services.AddScoped<IUserAccessControlService, UserAccessControlService>();
        services.AddScoped<IAclCheckingService, AclCheckingService>();

        return services;
    }

    /// <summary>
    /// Enforces access-token validation (with optional refresh) on MVC routes; see <see cref="AccessTokenValidationMiddleware"/>.
    /// </summary>
    public static IApplicationBuilder UseAccessTokenValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AccessTokenValidationMiddleware>();
    }

    /// <summary>
    /// Redirects bare <c>/</c> requests to <c>/ACLChecking</c> (preserving the query string and any
    /// configured base path), so external login redirects with a wrong path still hit the ACL gate.
    /// Must run after <c>UseRouting</c>/<c>UseSession</c> and before <see cref="UseAccessTokenValidation"/>.
    /// </summary>
    public static IApplicationBuilder UseRootAclRedirect(this IApplicationBuilder app)
    {
        return app.Use((ctx, next) =>
        {
            if (ctx.Request.Path != "/")
                return next();

            var basePath = ctx.Request.PathBase.HasValue
                ? ctx.Request.PathBase.ToString().TrimEnd('/')
                : string.Empty;
            ctx.Response.Redirect($"{basePath}/ACLChecking{ctx.Request.QueryString}");
            return Task.CompletedTask;
        });
    }
}
