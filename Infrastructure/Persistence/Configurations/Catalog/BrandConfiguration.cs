using Domain.Entities.CatalogEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalog
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.ToTable(nameof(Brand), Domain.Const.Schemas.CATALOG);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.NameAR).IsRequired().HasMaxLength(200);
            builder.Property(e => e.NameEN).IsRequired().HasMaxLength(200);

            builder.HasIndex(e => e.NameEN);
        }
    }
}
