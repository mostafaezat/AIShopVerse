using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Cart
{
    public class CartConfiguration : IEntityTypeConfiguration<Domain.Entities.CartEntities.Cart>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.CartEntities.Cart> builder)
        {
            builder.ToTable(nameof(Domain.Entities.CartEntities.Cart), Domain.Const.Schemas.DEFAULT);

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => e.UserId);

            builder.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
