using RestaurantApp.Models;
namespace RestaurantApp.Services
{
    public interface IOtpService
    {
        Task<string> GenerateOtpCode();
        Task<string> GenerateVerificationToken();
        Task<bool> VerifyOtpCode(string email, string otpCode, string? purpose = null);
        Task SaveOtpCode(string email, string otpCode, string purpose, TimeSpan? expiry = null);
        Task SendOtp(string email,string otpCode);
        Task SendVerificationEmail(string email, string otpCode, string verificationLink);
    }
}