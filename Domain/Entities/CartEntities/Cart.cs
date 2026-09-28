using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CartEntities
{
    [Table(nameof(Cart), Schema = Schemas.DEFAULT)]
    public class Cart : AuditedEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
