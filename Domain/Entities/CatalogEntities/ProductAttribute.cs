using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(ProductAttribute), Schema = Schemas.CATALOG)]
    public class ProductAttribute : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Value { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
