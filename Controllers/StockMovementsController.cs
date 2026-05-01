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
public class StockMovementsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryService _inventoryService;

    public StockMovementsController(ApplicationDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(int? productId, StockMovementType? movementType)
    {
        var movements = _context.StockMovements
            .Include(s => s.Product)
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue)
        {
            movements = movements.Where(s => s.ProductId == productId.Value);
        }

        if (movementType.HasValue)
        {
            movements = movements.Where(s => s.MovementType == movementType.Value);
        }

        ViewData["ProductId"] = new SelectList(
            await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(),
            nameof(Product.ProductId),
            nameof(Product.Name),
            productId);

        ViewData["MovementType"] = movementType;

        return View(await movements.OrderByDescending(s => s.CreatedAt).Take(100).ToListAsync());
    }

    public async Task<IActionResult> Create(int? productId)
    {
        var viewModel = new StockMovementCreateViewModel
        {
            ProductId = productId ?? 0,
            MovementType = StockMovementType.StockIn,
            Quantity = 1,
            ProductOptions = await GetProductOptionsAsync(productId)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockMovementCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            viewModel.ProductOptions = await GetProductOptionsAsync(viewModel.ProductId);
            return View(viewModel);
        }

        try
        {
            await _inventoryService.ApplyStockMovementAsync(
                viewModel.ProductId,
                viewModel.MovementType,
                viewModel.Quantity,
                viewModel.Reason,
                User.FindFirstValue(ClaimTypes.NameIdentifier));

            TempData["StatusMessage"] = "Stock movement recorded.";
            return RedirectToAction(nameof(Index), new { productId = viewModel.ProductId });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            viewModel.ProductOptions = await GetProductOptionsAsync(viewModel.ProductId);
            return View(viewModel);
        }
    }

    private async Task<List<SelectListItem>> GetProductOptionsAsync(int? selectedId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem($"{p.Name} ({p.SKU}) - {p.QuantityInStock} in stock", p.ProductId.ToString(), selectedId == p.ProductId))
            .ToListAsync();
    }
}
