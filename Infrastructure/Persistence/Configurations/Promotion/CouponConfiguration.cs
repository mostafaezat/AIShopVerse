using Domain.Entities.PromotionEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Promotion
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.ToTable(nameof(Coupon), Domain.Const.Schemas.Sales);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.Code).IsUnique();

            builder.Property(e => e.DiscountValue).HasColumnType("decimal(18,2)");
            builder.Property(e => e.MinOrderValue).HasColumnType("decimal(18,2)");
        }
    }
}
