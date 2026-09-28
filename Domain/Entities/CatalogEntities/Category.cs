using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(Category), Schema = Schemas.CATALOG)]
    public class Category : AuditedEntity
    {
        [Required]
        [MaxLength(200)]
        public string NameAR { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string NameEN { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public string? ParentId { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(ParentId))]
        public Category? Parent { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
