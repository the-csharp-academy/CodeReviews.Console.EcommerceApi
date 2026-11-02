using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Persistant;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ProductSale> ProductsSales => Set<ProductSale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductSale>().HasKey(ps => new { ps.ProductId, ps.SaleId });


        // SEED
        modelBuilder.Entity<Category>().HasData(
        // Parent Categories
        new Category { Id = 1, Name = "Electronics", Slug = "electronics", ParentId = null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Category { Id = 2, Name = "Clothing", Slug = "clothing", ParentId = null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

        // Child Categories
        new Category { Id = 3, Name = "Smartphones", Slug = "smartphones", ParentId = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        new Category { Id = 4, Name = "Men's Wear", Slug = "mens-wear", ParentId = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
    );

        // 2. Seed Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "iPhone 15",
                Sku = "ELEC-IPH-15",
                Price = 999.99m,
                StockQuantity = 50,
                IsActive = true,
                CategoryId = 3,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Samsung Galaxy S24",
                Sku = "ELEC-SAM-S24",
                Price = 899.50m,
                StockQuantity = 30,
                IsActive = true,
                CategoryId = 3,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 3,
                Name = "Classic Cotton T-Shirt",
                Sku = "CLOT-MEN-TSH",
                Price = 19.99m,
                StockQuantity = 200,
                IsActive = true,
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // 3. Seed Sales
        modelBuilder.Entity<Sale>().HasData(
            new Sale
            {
                Id = 1,
                Quantity = 2,
                UnitPrice = 999.99m,
                TotalPrice = 1999.98m,
                SaleDate = new DateTime(2026, 1, 5, 10, 30, 0, DateTimeKind.Utc)
            },
            new Sale
            {
                Id = 2,
                Quantity = 3,
                UnitPrice = 19.99m,
                TotalPrice = 59.97m,
                SaleDate = new DateTime(2026, 1, 6, 14, 15, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<ProductSale>().HasData(
            new ProductSale
            { // iPhone 15 linked to Sale 1
                ProductId = 1,
                SaleId = 1,
                CreatedAt = new DateTime(2026, 1, 6, 14, 15, 0, DateTimeKind.Utc)
            },
            new ProductSale
            { // T-Shirt linked to Sale 2
                ProductId = 3,
                SaleId = 2,
                CreatedAt = new DateTime(2026, 1, 6, 14, 15, 0, DateTimeKind.Utc)
            }
        );
    }
}


