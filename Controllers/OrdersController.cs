using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.Models;
using SmartInventory.Services;
using SmartInventory.ViewModels;

namespace SmartInventory.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryService _inventoryService;

    public OrdersController(ApplicationDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var orders = _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .AsQueryable();

        if (Enum.TryParse<OrderStatus>(status, out var parsedStatus))
        {
            orders = orders.Where(o => o.Status == parsedStatus);
        }

        ViewData["Status"] = status;

        return View(await orders.OrderByDescending(o => o.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new OrderCreateViewModel
        {
            Items = CreateBlankOrderLines(),
            ProductOptions = await GetProductOptionsAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            viewModel.ProductOptions = await GetProductOptionsAsync();
            PadOrderLines(viewModel);
            return View(viewModel);
        }

        try
        {
            var order = await _inventoryService.CreateOrderAsync(viewModel, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["StatusMessage"] = viewModel.Status == OrderStatus.Completed
                ? "Order completed and stock was updated."
                : "Draft order created.";

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            viewModel.ProductOptions = await GetProductOptionsAsync();
            PadOrderLines(viewModel);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _inventoryService.CompleteOrderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["StatusMessage"] = "Order completed and stock was updated.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["StatusMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await _inventoryService.CancelOrderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["StatusMessage"] = "Draft order cancelled. Stock was not changed.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["StatusMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static List<OrderItemInputViewModel> CreateBlankOrderLines()
    {
        return Enumerable.Range(0, 5)
            .Select(_ => new OrderItemInputViewModel())
            .ToList();
    }

    private static void PadOrderLines(OrderCreateViewModel viewModel)
    {
        while (viewModel.Items.Count < 5)
        {
            viewModel.Items.Add(new OrderItemInputViewModel());
        }
    }

    private async Task<List<SelectListItem>> GetProductOptionsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem($"{p.Name} - {p.UnitPrice:C} ({p.QuantityInStock} in stock)", p.ProductId.ToString()))
            .ToListAsync();
    }
}
