using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.CatalogEntities
{
    [Table(nameof(Brand), Schema = Schemas.CATALOG)]
    public class Brand : AuditedEntity
    {
        [Required]
        [MaxLength(200)]
        public string NameAR { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string NameEN { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
