using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.ReviewEntities
{
    [Table(nameof(Review), Schema = Schemas.DEFAULT)]
    public class Review : AuditedEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = true;
    }
}
