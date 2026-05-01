using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInventory.Models;

public class Product
{
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Barcode { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 999999.99)]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 999999.99)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumStockLevel { get; set; }

    [Url]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [NotMapped]
    public bool IsLowStock => IsActive && QuantityInStock > 0 && QuantityInStock <= MinimumStockLevel;

    [NotMapped]
    public bool IsOutOfStock => IsActive && QuantityInStock == 0;

    [NotMapped]
    public decimal InventoryValue => QuantityInStock * CostPrice;

    [NotMapped]
    public decimal PotentialRevenue => QuantityInStock * UnitPrice;
}
