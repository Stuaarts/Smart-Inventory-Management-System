using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Models;

namespace SmartInventory.Data;

public static class DbInitializer
{
    private static readonly string[] RoleNames = ["Admin", "Manager", "Staff"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await context.Database.MigrateAsync();

        foreach (var roleName in RoleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        await SeedUserAsync(userManager, "admin@demo.com", "Admin");
        await SeedUserAsync(userManager, "manager@demo.com", "Manager");
        await SeedUserAsync(userManager, "staff@demo.com", "Staff");

        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var hairProducts = new Category { Name = "Hair Products", Description = "Retail products for salon and barber clients." };
        var tools = new Category { Name = "Tools", Description = "Reusable shop tools and supplies." };
        var accessories = new Category { Name = "Accessories", Description = "Small add-ons and customer items." };

        context.Categories.AddRange(hairProducts, tools, accessories);

        var supplier = new Supplier
        {
            Name = "Northline Supply Co.",
            ContactName = "Maya Chen",
            Email = "orders@northline.example",
            Phone = "416-555-0181",
            City = "Toronto",
            Province = "ON",
            Country = "Canada",
            PostalCode = "M5V 2T6"
        };

        var secondSupplier = new Supplier
        {
            Name = "Urban Retail Distributors",
            ContactName = "Daniel Moore",
            Email = "sales@urbanretail.example",
            Phone = "647-555-0144",
            City = "Mississauga",
            Province = "ON",
            Country = "Canada",
            PostalCode = "L5B 3C1"
        };

        context.Suppliers.AddRange(supplier, secondSupplier);

        context.Products.AddRange(
            new Product
            {
                Name = "Matte Styling Clay",
                Description = "High-hold styling clay with a matte finish.",
                SKU = "HAIR-CLAY-100",
                Barcode = "700100200001",
                CostPrice = 8.50m,
                UnitPrice = 17.99m,
                QuantityInStock = 18,
                MinimumStockLevel = 8,
                Category = hairProducts,
                Supplier = supplier
            },
            new Product
            {
                Name = "Tea Tree Shampoo",
                Description = "Daily shampoo for professional retail shelves.",
                SKU = "HAIR-SHAM-250",
                Barcode = "700100200002",
                CostPrice = 6.25m,
                UnitPrice = 14.50m,
                QuantityInStock = 4,
                MinimumStockLevel = 10,
                Category = hairProducts,
                Supplier = supplier
            },
            new Product
            {
                Name = "Cape Clips Pack",
                Description = "Replacement clips for cutting capes.",
                SKU = "TOOL-CLIPS-12",
                Barcode = "700100200003",
                CostPrice = 3.10m,
                UnitPrice = 7.99m,
                QuantityInStock = 0,
                MinimumStockLevel = 5,
                Category = tools,
                Supplier = secondSupplier
            },
            new Product
            {
                Name = "Travel Comb Set",
                Description = "Compact comb set for checkout counter sales.",
                SKU = "ACC-COMB-TRAVEL",
                Barcode = "700100200004",
                CostPrice = 2.15m,
                UnitPrice = 5.99m,
                QuantityInStock = 32,
                MinimumStockLevel = 12,
                Category = accessories,
                Supplier = secondSupplier
            });

        await context.SaveChangesAsync();

        var admin = await userManager.FindByEmailAsync("admin@demo.com");
        var shampoo = await context.Products.FirstAsync(p => p.SKU == "HAIR-SHAM-250");
        var clay = await context.Products.FirstAsync(p => p.SKU == "HAIR-CLAY-100");

        context.StockMovements.AddRange(
            new StockMovement
            {
                ProductId = shampoo.ProductId,
                MovementType = StockMovementType.StockOut,
                Quantity = 6,
                PreviousQuantity = 10,
                NewQuantity = 4,
                Reason = "Opening sample sale",
                CreatedByUserId = admin?.Id
            },
            new StockMovement
            {
                ProductId = clay.ProductId,
                MovementType = StockMovementType.StockIn,
                Quantity = 18,
                PreviousQuantity = 0,
                NewQuantity = 18,
                Reason = "Opening stock",
                CreatedByUserId = admin?.Id
            });

        context.AuditLogs.Add(new AuditLog
        {
            UserId = admin?.Id,
            Action = "Seed data",
            EntityName = "Database",
            Description = "Created demo categories, suppliers, products, and stock movements."
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(UserManager<IdentityUser> userManager, string email, string roleName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Demo123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create demo user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
