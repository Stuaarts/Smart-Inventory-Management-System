using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.Models;
using SmartInventory.ViewModels;

namespace SmartInventory.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;

    public InventoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalProducts = products.Count,
            TotalCategories = await _context.Categories.CountAsync(c => c.IsActive),
            TotalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive),
            LowStockCount = products.Count(p => p.IsLowStock),
            OutOfStockCount = products.Count(p => p.IsOutOfStock),
            TotalInventoryValue = products.Sum(p => p.InventoryValue),
            PotentialRevenue = products.Sum(p => p.PotentialRevenue),
            TotalOrders = await _context.Orders.CountAsync(),
            LowStockProducts = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.IsActive && p.QuantityInStock <= p.MinimumStockLevel)
                .OrderBy(p => p.QuantityInStock)
                .ThenBy(p => p.Name)
                .Take(8)
                .ToListAsync(),
            RecentStockMovements = await _context.StockMovements
                .Include(s => s.Product)
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .Take(8)
                .ToListAsync(),
            RecentOrders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync()
        };
    }

    public async Task<StockMovement> ApplyStockMovementAsync(
        int productId,
        StockMovementType movementType,
        int quantity,
        string? reason,
        string? userId)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
        if (product is null)
        {
            throw new InvalidOperationException("Product was not found or is inactive.");
        }

        var previousQuantity = product.QuantityInStock;
        var newQuantity = movementType switch
        {
            StockMovementType.StockIn => previousQuantity + quantity,
            StockMovementType.Return => previousQuantity + quantity,
            StockMovementType.StockOut => previousQuantity - quantity,
            StockMovementType.Sale => previousQuantity - quantity,
            StockMovementType.Damaged => previousQuantity - quantity,
            StockMovementType.Adjustment => quantity,
            _ => previousQuantity
        };

        if (newQuantity < 0)
        {
            throw new InvalidOperationException("You cannot remove more stock than the product currently has.");
        }

        product.QuantityInStock = newQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            ProductId = product.ProductId,
            MovementType = movementType,
            Quantity = quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            Reason = reason,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockMovements.Add(movement);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Stock movement",
            EntityName = nameof(Product),
            EntityId = product.ProductId.ToString(),
            Description = $"{movementType} changed {product.Name} from {previousQuantity} to {newQuantity}."
        });

        await _context.SaveChangesAsync();

        return movement;
    }
}
