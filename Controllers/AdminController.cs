using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Data;
using SmartInventory.ViewModels;

namespace SmartInventory.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var viewModels = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? user.Id,
                EmailConfirmed = user.EmailConfirmed,
                Roles = string.Join(", ", roles),
                LockoutEnd = user.LockoutEnd
            });
        }

        return View(viewModels);
    }

    public async Task<IActionResult> AuditLogs()
    {
        var logs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync();

        return View(logs);
    }
}
