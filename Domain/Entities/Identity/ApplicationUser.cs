using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Domain.Const;
using Domain.Entities.OrderEntities;
using Domain.Entities.WishlistEntities;
using Domain.Entities.NotificationEntities;

namespace Domain.Entities.Identity
{
    [Table(nameof(ApplicationUser), Schema = Schemas.Identity)]
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<Domain.Entities.NotificationEntities.Notification> Notifications { get; set; } = new List<Domain.Entities.NotificationEntities.Notification>();
    }
}
