using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Services;

namespace SmartInventory.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IInventoryService _inventoryService;

    public DashboardController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = await _inventoryService.GetDashboardAsync();
        return View(viewModel);
    }
}
