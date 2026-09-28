using Domain.Entities.CatalogEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalog
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable(nameof(Product), Domain.Const.Schemas.CATALOG);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.NameAR).IsRequired().HasMaxLength(200);
            builder.Property(e => e.NameEN).IsRequired().HasMaxLength(200);
            builder.Property(e => e.SKU).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
            builder.Property(e => e.DiscountPrice).HasColumnType("decimal(18,2)");

            builder.HasIndex(e => e.SKU).IsUnique();
            builder.HasIndex(e => e.NameEN);
            builder.HasIndex(e => e.CategoryId);
            builder.HasIndex(e => e.BrandId);

            builder.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Attributes)
                .WithOne(a => a.Product)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
