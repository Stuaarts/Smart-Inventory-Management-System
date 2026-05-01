using System.ComponentModel.DataAnnotations;

namespace SmartInventory.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    public string? UserId { get; set; }

    [Required]
    [StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityId { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
