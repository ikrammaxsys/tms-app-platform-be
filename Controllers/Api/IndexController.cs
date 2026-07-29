using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/index")]
public class IndexController : ControllerBase
{
    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new
        {
            message = "TMS API is running",
            version = "1.0.0",
            success = true,
            code = HttpStatusCode.OK
        });
    }
}
