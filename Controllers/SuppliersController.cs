using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.Models;

namespace SmartInventory.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Products)
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(suppliers);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .Include(s => s.Products.OrderBy(p => p.Name))
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.SupplierId == id);

        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        return View(new Supplier { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([Bind("Name,ContactName,Email,Phone,Address,City,Province,Country,PostalCode,IsActive")] Supplier supplier)
    {
        if (!ModelState.IsValid)
        {
            return View(supplier);
        }

        supplier.CreatedAt = DateTime.UtcNow;
        _context.Add(supplier);
        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Create", nameof(Supplier), supplier.SupplierId.ToString(), $"Created supplier {supplier.Name}.");

        TempData["StatusMessage"] = "Supplier created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id, [Bind("SupplierId,Name,ContactName,Email,Phone,Address,City,Province,Country,PostalCode,IsActive,CreatedAt")] Supplier supplier)
    {
        if (id != supplier.SupplierId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(supplier);
        }

        var existingSupplier = await _context.Suppliers.FindAsync(id);
        if (existingSupplier is null)
        {
            return NotFound();
        }

        existingSupplier.Name = supplier.Name;
        existingSupplier.ContactName = supplier.ContactName;
        existingSupplier.Email = supplier.Email;
        existingSupplier.Phone = supplier.Phone;
        existingSupplier.Address = supplier.Address;
        existingSupplier.City = supplier.City;
        existingSupplier.Province = supplier.Province;
        existingSupplier.Country = supplier.Country;
        existingSupplier.PostalCode = supplier.PostalCode;
        existingSupplier.IsActive = supplier.IsActive;
        existingSupplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Update", nameof(Supplier), supplier.SupplierId.ToString(), $"Updated supplier {supplier.Name}.");

        TempData["StatusMessage"] = "Supplier updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.SupplierId == id);

        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _context.Suppliers.Include(s => s.Products).FirstOrDefaultAsync(s => s.SupplierId == id);
        if (supplier is null)
        {
            return NotFound();
        }

        if (supplier.Products.Any())
        {
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            TempData["StatusMessage"] = "Supplier has products, so it was marked inactive instead of deleted.";
        }
        else
        {
            _context.Suppliers.Remove(supplier);
            TempData["StatusMessage"] = "Supplier deleted.";
        }

        await _context.SaveChangesAsync();
        await AddAuditLogAsync("Delete", nameof(Supplier), id.ToString(), $"Deleted or deactivated supplier {supplier.Name}.");

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
