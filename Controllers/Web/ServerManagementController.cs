using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.ViewModels;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Web;
[Route("[controller]")]
public class ServerManagementController : Controller
{
    private readonly IServerService _serverService;
    public ServerManagementController(IServerService serverService)
    {
        _serverService = serverService;
    }
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var servers = await _serverService.GetAllAsync(cancellationToken);
        var model = new DataTableViewModel
        {
            Id = "serverTable",
            RouteTemplate = "~/ServerManagement/Detail/{id}",
            RowClickRedirect = true,
            IncludeCheckbox = true,
            Columns =
            [
                new() { Data = "id", Title = "ID", Width = "7%" },
                new() { Data = "domain", Title = "Domain", Width = "22%" },
                new() { Data = "ipAddress", Title = "IP Address", Width = "14%" },
                new() { Data = "environment", Title = "Environment", Width = "12%" },
                new() { Data = "internalExternal", Title = "Internal / External", Width = "14%" },
                new() { Data = "country", Title = "Country", Width = "10%" },
                new() { Data = "provider", Title = "Provider", Width = "12%" }
            ],
            Rows = servers.Select(x => new Dictionary<string, object?>
            {
                ["id"] = x.Id,
                ["domain"] = x.Domain,
                ["ipAddress"] = x.IpAddress,
                ["environment"] = x.Environment,
                ["internalExternal"] = x.InternalExternal,
                ["country"] = x.Country,
                ["provider"] = x.Provider
            }).ToList()
        };
        return View(model);
    }
    [HttpGet("Create")]
    public IActionResult Create() => View();
    [HttpGet("Detail/{id:int}")]
    public IActionResult Detail(int id)
    {
        ViewBag.Id = id;
        return View();
    }
    [HttpGet("Edit/{id:int}")]
    public IActionResult Edit(int id)
    {
        ViewBag.Id = id;
        return View();
    }
}
