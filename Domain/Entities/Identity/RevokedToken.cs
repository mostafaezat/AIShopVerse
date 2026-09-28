using System.ComponentModel.DataAnnotations.Schema;
using Domain.Const;
using Domain.Entities.BaseEntities;

namespace Domain.Entities.Identity
{
    [Table(nameof(RevokedToken), Schema = Schemas.Identity)]
    public class RevokedToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
    }
}
