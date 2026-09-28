using Domain.Entities.CatalogEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalog
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.ToTable(nameof(ProductVariant), Domain.Const.Schemas.CATALOG);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.SKU).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
            builder.Property(e => e.Size).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Color).IsRequired().HasMaxLength(50);

            builder.HasIndex(e => e.SKU).IsUnique();
            builder.HasIndex(e => new { e.ProductId, e.Size, e.Color }).IsUnique();
        }
    }
}
