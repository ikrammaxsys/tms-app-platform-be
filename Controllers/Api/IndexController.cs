using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Integrations.ExternalApi;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/index")]
public class IndexController : ControllerBase
{
    private readonly ILogger<IndexController> _logger;
    private readonly IACLService _aclService;

    public IndexController(ILogger<IndexController> logger, IACLService aclService)
    {
        _logger = logger;
        _aclService = aclService;
    }

    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new { 
            message = "TMS API is running",
            version = "1.0.0", success = true,
            code = HttpStatusCode.OK
        });
    }

    [HttpGet("sidebar")]
    public async Task<IActionResult> Sidebar([FromQuery] string systemName)
    {   
        var idAclUser = HttpContext.Session.GetString("ID_ACL_USER")?.Trim() ?? "";
        var sidebarResponse = await _aclService.GetSidebar(idAclUser, systemName);
        return Ok(ApiResponse<dynamic>.SuccessResponse(sidebarResponse));
    }
}
