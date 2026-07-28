using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using tms_template_net8.Auth.Models;
using tms_template_net8.Auth.Services;

namespace tms_template_net8.Auth.Security;

/// <summary>
/// Declarative page-level access control. Decorate a controller or action with this attribute
/// to require that the current session's <c>UserAclData.AccessControls</c> contains the named
/// resource with the specified <see cref="AccessRight"/>.
/// </summary>
/// <example>
/// <code>
/// [RequirePageAccess("PAB Sites", AccessRight.View)]
/// public class PabSitesController : Controller { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePageAccessAttribute : TypeFilterAttribute
{
    public RequirePageAccessAttribute(string accessName, AccessRight right = AccessRight.View)
        : base(typeof(PageAccessAuthorizationFilter))
    {
        if (string.IsNullOrWhiteSpace(accessName))
            throw new ArgumentException("Access name is required.", nameof(accessName));

        AccessName = accessName;
        Right = right;
        Arguments = new object[] { accessName, right };
    }

    public string AccessName { get; }
    public AccessRight Right { get; }
}

/// <summary>
/// Authorization filter that backs <see cref="RequirePageAccessAttribute"/>.
/// Reads the cached <see cref="UserAclData"/> from session and either lets the request
/// proceed or short-circuits with a redirect to <c>/Home/AccessDenied</c>.
/// </summary>
public sealed class PageAccessAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string _accessName;
    private readonly AccessRight _right;
    private readonly IUserAccessControlService _accessControlService;

    public PageAccessAuthorizationFilter(
        string accessName,
        AccessRight right,
        IUserAccessControlService accessControlService)
    {
        _accessName = accessName;
        _right = right;
        _accessControlService = accessControlService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        await http.Session.LoadAsync(http.RequestAborted).ConfigureAwait(false);

        if (_accessControlService.HasAccess(http, _accessName, _right))
            return;

        var basePath = http.Request.PathBase.HasValue
            ? http.Request.PathBase.ToString().TrimEnd('/')
            : string.Empty;

        var url = $"{basePath}/Home/AccessDenied?name={Uri.EscapeDataString(_accessName)}&right={Uri.EscapeDataString(_right.ToToken())}";
        context.Result = new RedirectResult(url);
    }
}
