namespace SmartInventory.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string Roles { get; set; } = string.Empty;

    public DateTimeOffset? LockoutEnd { get; set; }
}
