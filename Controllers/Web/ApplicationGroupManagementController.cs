using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.ViewModels;
using tms_template_net8.Services;
namespace tms_template_net8.Controllers.Web;
[Route("[controller]")]
public class ApplicationGroupManagementController : Controller
{
    private readonly IApplicationGroupService _groupService;
    public ApplicationGroupManagementController(IApplicationGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetAllAsync(cancellationToken);
        var model = new DataTableViewModel
        {
            Id = "applicationGroupTable",
            RouteTemplate = "~/ApplicationGroupManagement/Detail/{id}",
            RowClickRedirect = true,
            IncludeCheckbox = true,
            Columns =
            [
                new() { Data = "id", Title = "ID", Width = "15%" },
                new() { Data = "name", Title = "Group Name", Width = "85%" }
            ],
            Rows = groups.Select(x => new Dictionary<string, object?>
            {
                ["id"] = x.Id,
                ["name"] = x.Name
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
