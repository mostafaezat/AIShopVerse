using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(ProductImage), Schema = Schemas.CATALOG)]
    public class ProductImage : BaseEntity
    {
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }

        public string ProductId { get; set; } = string.Empty;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
