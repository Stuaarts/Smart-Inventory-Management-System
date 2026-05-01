using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartInventory.Models;

namespace SmartInventory.ViewModels;

public class StockMovementCreateViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Display(Name = "Movement Type")]
    public StockMovementType MovementType { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    public List<SelectListItem> ProductOptions { get; set; } = new();
}
