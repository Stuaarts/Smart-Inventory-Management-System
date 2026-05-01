using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.Models;

namespace SmartInventory.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .Include(c => c.Products.OrderBy(p => p.Name))
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == id);

        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        return View(new Category { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Category category)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        category.CreatedAt = DateTime.UtcNow;
        _context.Add(category);
        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Create", nameof(Category), category.CategoryId.ToString(), $"Created category {category.Name}.");

        TempData["StatusMessage"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await _context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description,IsActive,CreatedAt")] Category category)
    {
        if (id != category.CategoryId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        var existingCategory = await _context.Categories.FindAsync(id);
        if (existingCategory is null)
        {
            return NotFound();
        }

        existingCategory.Name = category.Name;
        existingCategory.Description = category.Description;
        existingCategory.IsActive = category.IsActive;
        existingCategory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Update", nameof(Category), category.CategoryId.ToString(), $"Updated category {category.Name}.");

        TempData["StatusMessage"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == id);

        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Products.Any())
        {
            ModelState.AddModelError(string.Empty, "This category cannot be deleted while products are assigned to it.");
            return View(category);
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Delete", nameof(Category), id.ToString(), $"Deleted category {category.Name}.");

        TempData["StatusMessage"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
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
