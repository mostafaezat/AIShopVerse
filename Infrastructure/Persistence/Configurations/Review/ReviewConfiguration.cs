using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Review
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Domain.Entities.ReviewEntities.Review>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.ReviewEntities.Review> builder)
        {
            builder.ToTable(nameof(Domain.Entities.ReviewEntities.Review), Domain.Const.Schemas.DEFAULT);

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();

            builder.HasIndex(e => e.ProductId);
        }
    }
}
