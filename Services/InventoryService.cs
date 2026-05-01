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

    public async Task<Order> CreateOrderAsync(OrderCreateViewModel viewModel, string? userId)
    {
        var requestedItems = viewModel.Items
            .Where(i => i.ProductId.HasValue && i.Quantity > 0)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g => new OrderItemInputViewModel
            {
                ProductId = g.Key,
                Quantity = g.Sum(i => i.Quantity)
            })
            .ToList();

        if (!requestedItems.Any())
        {
            throw new InvalidOperationException("Add at least one product to create an order.");
        }

        if (viewModel.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Create orders as Draft or Completed. Cancel drafts from the order details page.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var products = await _context.Products
            .Where(p => requestedItems.Select(i => i.ProductId!.Value).Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            CustomerName = viewModel.CustomerName,
            CustomerEmail = viewModel.CustomerEmail,
            Status = OrderStatus.Draft,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow
        };

        // Bookmark: prices are captured here so old orders keep their original selling price.
        foreach (var requestedItem in requestedItems)
        {
            var productId = requestedItem.ProductId!.Value;
            if (!products.TryGetValue(productId, out var product) || !product.IsActive)
            {
                throw new InvalidOperationException("One of the selected products is no longer available.");
            }

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.ProductId,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = product.UnitPrice * requestedItem.Quantity
            });
        }

        order.TotalAmount = order.OrderItems.Sum(i => i.LineTotal);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        if (viewModel.Status == OrderStatus.Completed)
        {
            CompleteLoadedOrder(order, products, userId);
            await _context.SaveChangesAsync();
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Create",
            EntityName = nameof(Order),
            EntityId = order.OrderId.ToString(),
            Description = $"Created order {order.OrderNumber}."
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return order;
    }

    public async Task<Order> CompleteOrderAsync(int orderId, string? userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
        {
            throw new InvalidOperationException("Order was not found.");
        }

        if (order.Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be completed.");
        }

        var products = order.OrderItems
            .Where(i => i.Product is not null)
            .ToDictionary(i => i.ProductId, i => i.Product!);

        CompleteLoadedOrder(order, products, userId);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return order;
    }

    public async Task<Order> CancelOrderAsync(int orderId, string? userId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order is null)
        {
            throw new InvalidOperationException("Order was not found.");
        }

        if (order.Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Cancel",
            EntityName = nameof(Order),
            EntityId = order.OrderId.ToString(),
            Description = $"Cancelled order {order.OrderNumber} before stock was reduced."
        });

        await _context.SaveChangesAsync();

        return order;
    }

    private void CompleteLoadedOrder(Order order, Dictionary<int, Product> products, string? userId)
    {
        if (!order.OrderItems.Any())
        {
            throw new InvalidOperationException("Order must have at least one item.");
        }

        foreach (var item in order.OrderItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
            {
                throw new InvalidOperationException("One of the products on this order is no longer active.");
            }

            if (product.QuantityInStock < item.Quantity)
            {
                throw new InvalidOperationException($"{product.Name} only has {product.QuantityInStock} unit(s) available.");
            }
        }

        // Bookmark: completing an order is the only place sales reduce inventory.
        foreach (var item in order.OrderItems)
        {
            var product = products[item.ProductId];
            var previousQuantity = product.QuantityInStock;
            product.QuantityInStock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.ProductId,
                MovementType = StockMovementType.Sale,
                Quantity = item.Quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = product.QuantityInStock,
                Reason = $"Completed order {order.OrderNumber}",
                CreatedByUserId = userId
            });
        }

        order.Status = OrderStatus.Completed;
        order.OrderDate = DateTime.UtcNow;
        order.TotalAmount = order.OrderItems.Sum(i => i.LineTotal);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Complete",
            EntityName = nameof(Order),
            EntityId = order.OrderId.ToString(),
            Description = $"Completed order {order.OrderNumber} and reduced stock."
        });
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        string orderNumber;
        do
        {
            orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }
        while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber));

        return orderNumber;
    }
}
