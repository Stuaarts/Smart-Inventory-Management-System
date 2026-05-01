using System.ComponentModel.DataAnnotations;

namespace SmartInventory.Models;

public enum StockMovementType
{
    [Display(Name = "Stock In")]
    StockIn,

    [Display(Name = "Stock Out")]
    StockOut,

    Adjustment,

    Sale,

    Return,

    Damaged
}
