using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartInventory.Models;

namespace SmartInventory.ViewModels;

public class OrderCreateViewModel
{
    [StringLength(120)]
    [Display(Name = "Customer Name")]
    public string? CustomerName { get; set; }

    [EmailAddress]
    [StringLength(150)]
    [Display(Name = "Customer Email")]
    public string? CustomerEmail { get; set; }

    [Display(Name = "Order Status")]
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public List<OrderItemInputViewModel> Items { get; set; } = new();

    public List<SelectListItem> ProductOptions { get; set; } = new();
}
