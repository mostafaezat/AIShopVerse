using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CartEntities
{
    [Table(nameof(CartItem), Schema = Schemas.DEFAULT)]
    public class CartItem : BaseEntity
    {
        public string CartId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string? VariantId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
