using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Domain.Const;

namespace Domain.Entities.Identity
{
    [Table(nameof(ApplicationRole), Schema = Schemas.Identity)]
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
    }
}
