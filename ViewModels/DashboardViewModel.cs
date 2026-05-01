using SmartInventory.Models;

namespace SmartInventory.ViewModels;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }

    public int TotalCategories { get; set; }

    public int TotalSuppliers { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public decimal TotalInventoryValue { get; set; }

    public decimal PotentialRevenue { get; set; }

    public int TotalOrders { get; set; }

    public List<Product> LowStockProducts { get; set; } = new();

    public List<StockMovement> RecentStockMovements { get; set; } = new();

    public List<Order> RecentOrders { get; set; } = new();
}
