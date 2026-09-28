using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;
using Domain.Entities.ReviewEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(Product), Schema = Schemas.CATALOG)]
    public class Product : AuditedEntity
    {
        [Required]
        [MaxLength(200)]
        public string NameAR { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string NameEN { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public int? LowStockThreshold { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? DescriptionAR { get; set; }

        [MaxLength(2000)]
        public string? DescriptionEN { get; set; }

        public string CategoryId { get; set; } = string.Empty;

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        public string? BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
