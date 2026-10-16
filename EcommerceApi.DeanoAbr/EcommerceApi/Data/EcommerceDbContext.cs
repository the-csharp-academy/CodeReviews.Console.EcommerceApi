using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data;

public class EcommerceDbContext : DbContext
{
    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(80);
            entity.Property(c => c.Description).HasMaxLength(400);
            // Deliberately not unique: a soft-deleted row keeps its name, so a unique index would
            // block re-creating it forever. Uniqueness is enforced against active rows in the services.
            entity.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(120);
            entity.Property(p => p.Description).HasMaxLength(400);
            entity.Property(p => p.Price).HasPrecision(10, 2);
            entity.HasIndex(p => p.Name);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict); // retail: never cascade away a category's products
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.CustomerName).IsRequired().HasMaxLength(120);

            entity.HasMany(s => s.SaleItems)
                  .WithOne(i => i.Sale!)
                  .HasForeignKey(i => i.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(120);
            entity.Property(i => i.UnitPrice).HasPrecision(10, 2);

            entity.HasOne(i => i.Product)
                  .WithMany(p => p.SaleItems)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict); // keep sales history when a product is retired
        });
    }
}
