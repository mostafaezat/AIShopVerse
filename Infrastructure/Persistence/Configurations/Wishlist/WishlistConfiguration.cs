using Domain.Entities.WishlistEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Wishlist
{
    public class WishlistConfiguration : IEntityTypeConfiguration<Domain.Entities.WishlistEntities.WishlistItem>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.WishlistEntities.WishlistItem> builder)
        {
            builder.ToTable(nameof(Domain.Entities.WishlistEntities.WishlistItem), Domain.Const.Schemas.DEFAULT);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            builder.Property(e => e.ProductId).IsRequired().HasMaxLength(450);

            builder.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            builder.HasIndex(e => e.UserId);
        }
    }
}
