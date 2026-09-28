using Domain.Entities.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Payment
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Domain.Entities.PaymentEntities.Payment>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.PaymentEntities.Payment> builder)
        {
            builder.ToTable(nameof(Domain.Entities.PaymentEntities.Payment), Domain.Const.Schemas.Sales);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            builder.Property(e => e.Method).IsRequired().HasMaxLength(50);

            builder.HasIndex(e => e.OrderId);
        }
    }
}
