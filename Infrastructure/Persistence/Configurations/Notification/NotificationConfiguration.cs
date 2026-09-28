using Domain.Entities.NotificationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notification
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Domain.Entities.NotificationEntities.Notification>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.NotificationEntities.Notification> builder)
        {
            builder.ToTable(nameof(Domain.Entities.NotificationEntities.Notification), Domain.Const.Schemas.DEFAULT);

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
            builder.Property(e => e.Message).HasMaxLength(1000);

            builder.HasIndex(e => new { e.UserId, e.IsRead });
            builder.HasIndex(e => e.UserId);
        }
    }
}
