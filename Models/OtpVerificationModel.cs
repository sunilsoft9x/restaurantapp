using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace RestaurantApp.Models
{
    [Index(nameof(Email))]
    [Index(nameof(HashedOtpCode))]
    public class OtpVerificationModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }
        [Required]
        [MaxLength(500)]
        public string HashedOtpCode { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; } = false; // To track if the OTP has been used
        public int AttemptCount { get; set; } = 0; // To track the number of verification attempts
        public int MaxAttempts { get; set; } = 3; // Maximum allowed attempts before locking out
        [MaxLength(50)]
        public string? Purpose { get; set; } // e.g., "PasswordReset", "EmailVerification"
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public bool IsDeleted { get; set; } = false; // Soft delete flag
        public DateTime? DeletedAt { get; set; }
        
    }
}