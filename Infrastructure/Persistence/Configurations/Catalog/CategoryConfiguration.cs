using Domain.Entities.CatalogEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalog
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable(nameof(Category), Domain.Const.Schemas.CATALOG);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.NameAR).IsRequired().HasMaxLength(200);
            builder.Property(e => e.NameEN).IsRequired().HasMaxLength(200);

            builder.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.NameEN);
        }
    }
}
