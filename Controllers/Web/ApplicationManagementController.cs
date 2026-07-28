using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.ViewModels;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Web;
[Route("[controller]")]
public class ApplicationManagementController : Controller
{
    private readonly IApplicationService _applicationService;
    public ApplicationManagementController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var apps = await _applicationService.GetAllAsync(cancellationToken);
        var model = new DataTableViewModel
        {
            Id = "applicationTable",
            RouteTemplate = "~/ApplicationManagement/Detail/{id}",
            RowClickRedirect = true,
            IncludeCheckbox = true,
            Columns =
            [
                new() { Data = "id", Title = "ID", Width = "6%" },
                new() { Data = "name", Title = "Application", Width = "14%" },
                new() { Data = "applicationGroupName", Title = "Group", Width = "10%" },
                new() { Data = "version", Title = "Version", Width = "9%" },
                new() { Data = "serverDomain", Title = "Server", Width = "16%" },
                new() { Data = "appUrl", Title = "App URL", Width = "18%" },
                new() { Data = "lastDeployment", Title = "Last Deployment", Width = "12%" }
            ],
            Rows = apps.Select(x => new Dictionary<string, object?>
            {
                ["id"] = x.Id,
                ["name"] = x.Name,
                ["applicationGroupName"] = x.ApplicationGroupName,
                ["version"] = x.Version,
                ["serverDomain"] = x.ServerDomain,
                ["appUrl"] = x.AppUrl,
                ["lastDeployment"] = x.LastDeployment?.ToString("yyyy-MM-dd HH:mm") ?? ""
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
