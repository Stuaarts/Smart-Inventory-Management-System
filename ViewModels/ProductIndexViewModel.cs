using Microsoft.AspNetCore.Mvc.Rendering;
using SmartInventory.Infrastructure;
using SmartInventory.Models;

namespace SmartInventory.ViewModels;

public class ProductIndexViewModel
{
    public PaginatedList<Product> Products { get; set; } = default!;

    public string? SearchString { get; set; }

    public int? CategoryId { get; set; }

    public int? SupplierId { get; set; }

    public string? StockStatus { get; set; }

    public string SortOrder { get; set; } = "name";

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public List<SelectListItem> SupplierOptions { get; set; } = new();

    public List<SelectListItem> StockStatusOptions { get; set; } = new()
    {
        new SelectListItem("In Stock", "in"),
        new SelectListItem("Low Stock", "low"),
        new SelectListItem("Out of Stock", "out"),
        new SelectListItem("Inactive", "inactive")
    };

    public List<SelectListItem> SortOptions { get; set; } = new()
    {
        new SelectListItem("Name A-Z", "name"),
        new SelectListItem("Name Z-A", "name_desc"),
        new SelectListItem("Price Low to High", "price"),
        new SelectListItem("Price High to Low", "price_desc"),
        new SelectListItem("Quantity Low to High", "quantity"),
        new SelectListItem("Quantity High to Low", "quantity_desc"),
        new SelectListItem("Newest First", "newest")
    };
}
