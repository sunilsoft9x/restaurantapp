using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Models
{
    public class RefreshTokenModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        public int UserId { get; set; }
        public UserModel User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }
}
