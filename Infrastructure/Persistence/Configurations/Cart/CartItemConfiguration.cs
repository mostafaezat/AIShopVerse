using Domain.Entities.CartEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Cart
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable(nameof(CartItem), Domain.Const.Schemas.DEFAULT);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.VariantId).HasMaxLength(36);
            builder.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

            builder.HasIndex(e => new { e.CartId, e.ProductId, e.VariantId });
        }
    }
}
