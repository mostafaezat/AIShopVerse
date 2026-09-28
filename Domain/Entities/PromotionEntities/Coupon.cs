using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.PromotionEntities
{
    public enum DiscountType
    {
        Percentage,
        Fixed
    }

    [Table(nameof(Coupon), Schema = Schemas.Sales)]
    public class Coupon : AuditedEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public DiscountType DiscountType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderValue { get; set; }

        public int MaxUses { get; set; } = 0;
        public int UsedCount { get; set; } = 0;

        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsValid =>
            IsActive &&
            DateTime.UtcNow >= ValidFrom &&
            DateTime.UtcNow <= ValidTo &&
            (MaxUses == 0 || UsedCount < MaxUses);
    }
}
