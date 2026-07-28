using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Auth.Models;
using tms_template_net8.Auth.Services;

namespace tms_template_net8.Controllers.Web;

[Route("[controller]")]
public class ACLCheckingController : Controller
{
    private readonly IAclCheckingService _aclCheckingService;

    public ACLCheckingController(IAclCheckingService aclCheckingService)
    {
        _aclCheckingService = aclCheckingService;
    }

    /// <summary>
    /// Entry URL from the external login app (canonical path), e.g.
    /// <c>https://host/ACLChecking/?ID_ACL_USER=7171&amp;auth-code=...</c>
    /// Requests to <c>/</c> with the same query are redirected here from <c>Program.cs</c>.
    /// <list type="number">
    /// <item><c>auth-code</c> is exchanged for JWT cookies, then stripped from the URL (other query params are kept).</item>
    /// <item>The view POSTs the access token to <see cref="Verify"/> for server-side validation and session.</item>
    /// </list>
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var authCode = Request.Query["auth-code"].ToString().Trim();
        var result = await _aclCheckingService.ProcessIndexAsync(HttpContext, authCode, HttpContext.RequestAborted);
        if (!string.IsNullOrEmpty(result.RedirectPathAndQuery))
            return LocalRedirect(result.RedirectPathAndQuery);

        ViewBag.Error = result.Error;
        ViewBag.TokenKey = result.TokenKey;
        return View();
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] AclTokenRequest? body)
    {
        var result = await _aclCheckingService.VerifyAsync(HttpContext, body, HttpContext.RequestAborted);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                redirectUrl = result.RedirectUrl
            });
        }

        return Ok(new
        {
            success = true,
            redirectUrl = result.RedirectUrl,
            userId = result.UserId,
            empName = result.UserId,
            userName = result.UserId
        });
    }

    /// <summary>
    /// Shows a full-page signing-out screen; the browser then POSTs to <see cref="Logout"/> to end the session.
    /// </summary>
    [HttpGet("logout")]
    public IActionResult LogoutPage()
    {
        var data = _aclCheckingService.GetLogoutPageData(HttpContext);
        ViewBag.TokenKey = data.TokenKey;
        ViewBag.DspBaseUrl = data.DspBaseUrl;
        ViewBag.LogoutPostUrl = Url.Action(nameof(Logout), "ACLChecking");
        return View();
    }

    /// <summary>
    /// Ends the app session, clears all cookies, and returns a redirect URL for the browser.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _aclCheckingService.Logout(HttpContext);

        return Ok(new
        {
            success = true,
            redirectUrl = Url.Action(nameof(Logout), "ACLChecking")
        });
    }
}
