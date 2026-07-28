using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Auth.Models;
using tms_template_net8.Auth.Security;
using tms_template_net8.Models.ViewModels;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Web;

// Controller-level requirement: any action below needs at least 'view' on the access-control resource.
// The string must match a key in the ACL `accessControls` dictionary returned by the auth API.
[Route("[controller]")]
[RequirePageAccess("PAB Sites", AccessRight.View)]
public class ProductManagementController : Controller
{
    private readonly IProductService _productService;
    public ProductManagementController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        var model = new DataTableViewModel
        {
            Id = "productTable",
            RouteTemplate = "~/ProductManagement/Detail/{id}",
            RowClickRedirect = true,
            IncludeCheckbox = true,
            Columns =
            [
                new() { Data = "id", Title = "ID", Width = "10%" },
                new() { Data = "name", Title = "Name", Width = "25%" },
                new() { Data = "sku", Title = "SKU", Width = "20%" },
                new() { Data = "price", Title = "Price", Width = "15%" },
                new() { Data = "status", Title = "Status", Width = "15%", RenderName = "__status" }
            ],
            Rows = _productService.GetAll().Select(x => new Dictionary<string, object?>
            {
                ["id"] = x.Id,
                ["name"] = x.Name,
                ["sku"] = x.Sku,
                ["price"] = x.Price.ToString("0.00"),
                ["status"] = x.Status
            }).ToList()
        };
        return View(model);
    }

    // Per-action override: requires 'add' on top of the controller-level 'view'.
    [HttpGet("Create")]
    [RequirePageAccess("PAB Sites", AccessRight.Add)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet("Detail/{id:int}")]
    public IActionResult Detail(int id)
    {
        ViewBag.Id = id;
        return View();
    }

    [HttpGet("Edit/{id:int}")]
    [RequirePageAccess("PAB Sites", AccessRight.Edit)]
    public IActionResult Edit(int id)
    {
        ViewBag.Id = id;
        return View();
    }
}
