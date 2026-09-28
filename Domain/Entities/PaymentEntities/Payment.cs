using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.PaymentEntities
{
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        CashOnDelivery,
        CreditCard
    }

    [Table(nameof(Payment), Schema = Schemas.Sales)]
    public class Payment : BaseEntity
    {
        public string OrderId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Method { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TransactionId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime? PaidAt { get; set; }

        [MaxLength(500)]
        public string? GatewayResponse { get; set; }
    }
}
