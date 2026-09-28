using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(ProductVariant), Schema = Schemas.CATALOG)]
    public class ProductVariant : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        [MaxLength(500)]
        public string? AttributeValues { get; set; }

        [MaxLength(50)]
        public string Size { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string ProductId { get; set; } = string.Empty;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
