using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInventory.Models;

public class Order
{
    public int OrderId { get; set; }

    [Required]
    [StringLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    [StringLength(120)]
    public string? CustomerName { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? CustomerEmail { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
