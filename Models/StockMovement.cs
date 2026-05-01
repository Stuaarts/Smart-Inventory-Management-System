using System.ComponentModel.DataAnnotations;

namespace SmartInventory.Models;

public class StockMovement
{
    public int StockMovementId { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public StockMovementType MovementType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
