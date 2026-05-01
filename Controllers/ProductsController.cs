using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.Infrastructure;
using SmartInventory.Models;
using SmartInventory.ViewModels;

namespace SmartInventory.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private const int PageSize = 8;
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? searchString,
        int? categoryId,
        int? supplierId,
        string? stockStatus,
        string? sortOrder,
        int pageNumber = 1)
    {
        var products = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            products = products.Where(p =>
                p.Name.Contains(searchString) ||
                p.SKU.Contains(searchString) ||
                (p.Barcode != null && p.Barcode.Contains(searchString)));
        }

        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        if (supplierId.HasValue)
        {
            products = products.Where(p => p.SupplierId == supplierId.Value);
        }

        products = stockStatus switch
        {
            "in" => products.Where(p => p.IsActive && p.QuantityInStock > p.MinimumStockLevel),
            "low" => products.Where(p => p.IsActive && p.QuantityInStock > 0 && p.QuantityInStock <= p.MinimumStockLevel),
            "out" => products.Where(p => p.IsActive && p.QuantityInStock == 0),
            "inactive" => products.Where(p => !p.IsActive),
            _ => products
        };

        products = sortOrder switch
        {
            "name_desc" => products.OrderByDescending(p => p.Name),
            "price" => products.OrderBy(p => p.UnitPrice),
            "price_desc" => products.OrderByDescending(p => p.UnitPrice),
            "quantity" => products.OrderBy(p => p.QuantityInStock),
            "quantity_desc" => products.OrderByDescending(p => p.QuantityInStock),
            "newest" => products.OrderByDescending(p => p.CreatedAt),
            _ => products.OrderBy(p => p.Name)
        };

        var viewModel = new ProductIndexViewModel
        {
            Products = await PaginatedList<Product>.CreateAsync(products, Math.Max(pageNumber, 1), PageSize),
            SearchString = searchString,
            CategoryId = categoryId,
            SupplierId = supplierId,
            StockStatus = stockStatus,
            SortOrder = sortOrder ?? "name",
            CategoryOptions = await GetCategoryOptionsAsync(categoryId),
            SupplierOptions = await GetSupplierOptionsAsync(supplierId)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> LowStock()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .Where(p => p.IsActive && p.QuantityInStock <= p.MinimumStockLevel)
            .OrderBy(p => p.QuantityInStock)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.StockMovements.OrderByDescending(s => s.CreatedAt).Take(10))
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProductId == id);

        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create()
    {
        await SetProductSelectListsAsync();
        return View(new Product { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([Bind("Name,Description,SKU,Barcode,CostPrice,UnitPrice,QuantityInStock,MinimumStockLevel,ImageUrl,IsActive,CategoryId,SupplierId")] Product product)
    {
        if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
        {
            ModelState.AddModelError(nameof(Product.SKU), "SKU must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await SetProductSelectListsAsync(product);
            return View(product);
        }

        product.CreatedAt = DateTime.UtcNow;
        _context.Add(product);
        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Create", nameof(Product), product.ProductId.ToString(), $"Created product {product.Name}.");

        TempData["StatusMessage"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        await SetProductSelectListsAsync(product);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,SKU,Barcode,CostPrice,UnitPrice,QuantityInStock,MinimumStockLevel,ImageUrl,IsActive,CategoryId,SupplierId,CreatedAt")] Product formProduct)
    {
        if (id != formProduct.ProductId)
        {
            return NotFound();
        }

        if (await _context.Products.AnyAsync(p => p.SKU == formProduct.SKU && p.ProductId != formProduct.ProductId))
        {
            ModelState.AddModelError(nameof(Product.SKU), "SKU must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await SetProductSelectListsAsync(formProduct);
            return View(formProduct);
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = formProduct.Name;
        product.Description = formProduct.Description;
        product.SKU = formProduct.SKU;
        product.Barcode = formProduct.Barcode;
        product.CostPrice = formProduct.CostPrice;
        product.UnitPrice = formProduct.UnitPrice;
        product.QuantityInStock = formProduct.QuantityInStock;
        product.MinimumStockLevel = formProduct.MinimumStockLevel;
        product.ImageUrl = formProduct.ImageUrl;
        product.IsActive = formProduct.IsActive;
        product.CategoryId = formProduct.CategoryId;
        product.SupplierId = formProduct.SupplierId;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Update", nameof(Product), product.ProductId.ToString(), $"Updated product {product.Name}.");

        TempData["StatusMessage"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProductId == id);

        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var hasHistory = await _context.StockMovements.AnyAsync(s => s.ProductId == id) ||
            await _context.OrderItems.AnyAsync(o => o.ProductId == id);

        if (hasHistory)
        {
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            TempData["StatusMessage"] = "Product has history, so it was marked inactive instead of deleted.";
        }
        else
        {
            _context.Products.Remove(product);
            TempData["StatusMessage"] = "Product deleted.";
        }

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Delete", nameof(Product), id.ToString(), $"Deleted or deactivated product {product.Name}.");

        return RedirectToAction(nameof(Index));
    }

    private async Task SetProductSelectListsAsync(Product? product = null)
    {
        ViewData["CategoryId"] = new SelectList(
            await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
            nameof(Category.CategoryId),
            nameof(Category.Name),
            product?.CategoryId);

        ViewData["SupplierId"] = new SelectList(
            await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(),
            nameof(Supplier.SupplierId),
            nameof(Supplier.Name),
            product?.SupplierId);
    }

    private async Task<List<SelectListItem>> GetCategoryOptionsAsync(int? selectedId)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString(), selectedId == c.CategoryId))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetSupplierOptionsAsync(int? selectedId)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.SupplierId.ToString(), selectedId == s.SupplierId))
            .ToListAsync();
    }

    private async Task AddAuditLogAsync(string action, string entityName, string entityId, string description)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description
        });

        await _context.SaveChangesAsync();
    }
}
