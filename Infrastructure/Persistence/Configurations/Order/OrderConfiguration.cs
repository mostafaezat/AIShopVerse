using Domain.Entities.OrderEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Order
{
    public class OrderConfiguration : IEntityTypeConfiguration<Domain.Entities.OrderEntities.Order>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.OrderEntities.Order> builder)
        {
            builder.ToTable(nameof(Domain.Entities.OrderEntities.Order), Domain.Const.Schemas.Sales);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.OrderNumber).IsUnique();
            builder.HasIndex(e => e.UserId);

            builder.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(e => e.Tax).HasColumnType("decimal(18,2)");
            builder.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
            builder.Property(e => e.Total).HasColumnType("decimal(18,2)");

            builder.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
