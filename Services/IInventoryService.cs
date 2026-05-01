using SmartInventory.Models;
using SmartInventory.ViewModels;

namespace SmartInventory.Services;

public interface IInventoryService
{
    Task<DashboardViewModel> GetDashboardAsync();

    Task<StockMovement> ApplyStockMovementAsync(
        int productId,
        StockMovementType movementType,
        int quantity,
        string? reason,
        string? userId);

    Task<Order> CreateOrderAsync(OrderCreateViewModel viewModel, string? userId);

    Task<Order> CompleteOrderAsync(int orderId, string? userId);

    Task<Order> CancelOrderAsync(int orderId, string? userId);
}
