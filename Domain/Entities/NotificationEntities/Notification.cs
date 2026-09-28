using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.NotificationEntities
{
    public enum NotificationType
    {
        OrderStatus,
        Sale,
        Info
    }

    [Table(nameof(Notification), Schema = Schemas.DEFAULT)]
    public class Notification : BaseEntity
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Message { get; set; }

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string? OrderId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
