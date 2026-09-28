using Domain.Entities.OrderEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Order
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable(nameof(OrderItem), Domain.Const.Schemas.Sales);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(e => e.VariantId).HasMaxLength(36);
            builder.Property(e => e.VariantLabel).HasMaxLength(100);
            builder.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
        }
    }
}
