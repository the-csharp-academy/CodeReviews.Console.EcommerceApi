using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data;

public static class DbSeeder
{
    /// <summary>
    /// Idempotent seed of the Bean &amp; Barrel Coffee Roasters catalogue.
    /// The stock quantities below are the POST-sale remainders: the seeded sales are historical
    /// records and are not deducted from stock again.
    /// </summary>
    public static async Task SeedAsync(EcommerceDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var coffee = new Category { Name = "Coffee", Description = "Single-origin and blended roasted beans." };
        var brewing = new Category { Name = "Brewing Equipment", Description = "Grinders, drippers, presses and machines." };
        var accessories = new Category { Name = "Accessories", Description = "Filters, scales, cups and storage." };
        var subscriptions = new Category { Name = "Subscriptions", Description = "Recurring coffee deliveries." };
        var merchandise = new Category { Name = "Merchandise", Description = "Branded apparel and gifts." };
        db.Categories.AddRange(coffee, brewing, accessories, subscriptions, merchandise);

        var products = new List<Product>
        {
            new() { Name = "Ethiopia Yirgacheffe 250g", Description = "Floral, citrus, light roast.", Price = 89.50m, StockQuantity = 40, Category = coffee, CreatedAtUtc = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Colombia Huila 250g", Description = "Caramel and red apple.", Price = 74.00m, StockQuantity = 58, Category = coffee, CreatedAtUtc = new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Brazil Cerrado 1kg", Description = "Chocolate and hazelnut, bulk bag.", Price = 249.00m, StockQuantity = 17, Category = coffee, CreatedAtUtc = new DateTime(2026, 1, 7, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Espresso House Blend 250g", Description = "Our signature dark roast.", Price = 69.00m, StockQuantity = 75, Category = coffee, CreatedAtUtc = new DateTime(2026, 1, 8, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Decaf Sumatra 250g", Description = "Swiss-water decaffeinated.", Price = 79.00m, StockQuantity = 12, Category = coffee, CreatedAtUtc = new DateTime(2026, 1, 9, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Hand Grinder H5", Description = "Stainless conical burr, 40 clicks.", Price = 1299.00m, StockQuantity = 7, Category = brewing, CreatedAtUtc = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Pour-over Dripper V60", Description = "Ceramic, size 02.", Price = 349.00m, StockQuantity = 14, Category = brewing, CreatedAtUtc = new DateTime(2026, 1, 11, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "French Press 600ml", Description = "Borosilicate glass and steel.", Price = 429.00m, StockQuantity = 11, Category = brewing, CreatedAtUtc = new DateTime(2026, 1, 12, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Paper Filters (100 pack)", Description = "Oxygen-bleached V60 filters.", Price = 99.00m, StockQuantity = 119, Category = accessories, CreatedAtUtc = new DateTime(2026, 1, 13, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Digital Coffee Scale", Description = "0.1 g resolution with timer.", Price = 599.00m, StockQuantity = 14, Category = accessories, CreatedAtUtc = new DateTime(2026, 1, 14, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Monthly Roast Subscription", Description = "Two bags delivered every month.", Price = 159.00m, StockQuantity = 199, Category = subscriptions, CreatedAtUtc = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc) },
            new() { Name = "Bean & Barrel Ceramic Mug", Description = "350 ml branded mug.", Price = 149.00m, StockQuantity = 58, Category = merchandise, CreatedAtUtc = new DateTime(2026, 1, 16, 8, 0, 0, DateTimeKind.Utc) },
        };
        db.Products.AddRange(products);

        db.Sales.AddRange(
            new Sale
            {
                CustomerName = "Amara Nkosi",
                SoldAtUtc = new DateTime(2026, 2, 3, 9, 15, 0, DateTimeKind.Utc),
                SaleItems =
                {
                    Line(products[0], 2),
                    Line(products[8], 1),
                }
            },
            new Sale
            {
                CustomerName = "Thabo Molefe",
                SoldAtUtc = new DateTime(2026, 2, 7, 14, 40, 0, DateTimeKind.Utc),
                SaleItems =
                {
                    Line(products[5], 1),
                    Line(products[6], 1),
                    Line(products[9], 1),
                }
            },
            new Sale
            {
                CustomerName = "Lerato Dlamini",
                SoldAtUtc = new DateTime(2026, 2, 12, 11, 5, 0, DateTimeKind.Utc),
                SaleItems =
                {
                    Line(products[2], 3),
                    Line(products[10], 1),
                    Line(products[11], 2),
                }
            });

        await db.SaveChangesAsync();
    }

    private static SaleItem Line(Product product, int quantity) => new()
    {
        Product = product,
        ProductName = product.Name,
        Quantity = quantity,
        UnitPrice = product.Price,
    };
}
