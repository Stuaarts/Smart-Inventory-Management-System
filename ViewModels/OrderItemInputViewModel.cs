using System.ComponentModel.DataAnnotations;

namespace SmartInventory.ViewModels;

public class OrderItemInputViewModel
{
    public int? ProductId { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}
