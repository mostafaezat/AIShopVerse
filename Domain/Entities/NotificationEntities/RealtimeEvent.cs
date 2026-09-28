using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.NotificationEntities
{
    public enum RealtimeEventKind
    {
        OrderStatus,
        NewOrder,
        LowStock,
        BackInStock,
        Info
    }

    [Table(nameof(RealtimeEvent), Schema = Schemas.Sales)]
    public class RealtimeEvent : BaseEntity
    {
        public RealtimeEventKind Kind { get; set; }

        [MaxLength(200)]
        public string? TargetUserId { get; set; }

        [MaxLength(2000)]
        public string? PayloadJson { get; set; }

        [MaxLength(50)]
        public string Source { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Consumer { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
