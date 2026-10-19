using Ecommerce.WebAPIs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.WebAPIs.Configurations
{
    public class SaleConfiguration : IEntityTypeConfiguration<SaleEntity>
    {
        public void Configure(EntityTypeBuilder<SaleEntity> builder)
        {
            builder.HasKey(s=>s.Id);
        }
    }
}
