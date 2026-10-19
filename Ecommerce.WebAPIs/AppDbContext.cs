using Ecommerce.WebAPIs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebAPIs
{
    public class AppDbContext : DbContext
    {
        public DbSet<ProductEntity> Products { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }
        public DbSet<SaleEntity> Sales { get; set; }
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
