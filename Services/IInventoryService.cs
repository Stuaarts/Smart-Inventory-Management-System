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
}
