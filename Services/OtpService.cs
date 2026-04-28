using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Services
{
    public class OtpService : IOtpService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OtpService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public Task<string> GenerateOtpCode()
        {
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            return Task.FromResult(otp);
        }

        public Task<string> GenerateVerificationToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Task.FromResult(Convert.ToHexString(bytes).ToLowerInvariant());
        }

        public async Task SaveOtpCode(string email, string otpCode, string purpose, TimeSpan? expiry = null)
        {
            // Invalidate any previous unused OTPs for this email+purpose
            var existing = await _context.OtpVerifications
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed && !o.IsDeleted)
                .ToListAsync();

            foreach (var old in existing)
            {
                old.IsDeleted = true;
                old.DeletedAt = DateTime.UtcNow;
            }

            var hashed = HashOtp(otpCode);

            _context.OtpVerifications.Add(new OtpVerificationModel
            {
                Email = email,
                HashedOtpCode = hashed,
                ExpiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(10)),
                Purpose = purpose,
                IsUsed = false,
                AttemptCount = 0,
                MaxAttempts = 3
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> VerifyOtpCode(string email, string otpCode, string? purpose = null)
        {
            var hashed = HashOtp(otpCode);

            var query = _context.OtpVerifications
                .Where(o => o.Email == email && !o.IsUsed && !o.IsDeleted);

            if (!string.IsNullOrWhiteSpace(purpose))
            {
                query = query.Where(o => o.Purpose == purpose);
            }

            var record = await query
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
                return false;

            if (record.ExpiryTime < DateTime.UtcNow)
                return false;

            if (record.AttemptCount >= record.MaxAttempts)
                return false;

            record.AttemptCount++;

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(record.HashedOtpCode),
                Encoding.UTF8.GetBytes(hashed)))
            {
                await _context.SaveChangesAsync();
                return false;
            }

            record.IsUsed = true;
            record.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SendOtp(string email, string otpCode)
        {
            var subject = "Your RestaurantApp Verification Code";
            var body = $@"
                <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'>
                    <h2 style='color:#e65c00;'>RestaurantApp</h2>
                    <p>Your one-time verification code is:</p>
                    <h1 style='letter-spacing:8px;color:#333;'>{otpCode}</h1>
                    <p>This code expires in <strong>10 minutes</strong>. Do not share it with anyone.</p>
                    <hr style='border:none;border-top:1px solid #eee;'/>
                    <p style='font-size:12px;color:#999;'>If you did not request this, please ignore this email.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task SendVerificationEmail(string email, string otpCode, string verificationLink)
        {
            var subject = "Verify Your RestaurantApp Email";
            var body = $@"
                <div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'>
                    <h2 style='color:#e65c00;'>RestaurantApp</h2>
                    <p>Verify your account using either method below:</p>
                    <p>OTP code:</p>
                    <h1 style='letter-spacing:8px;color:#333;'>{otpCode}</h1>
                    <p>Or click this verification link:</p>
                    <p><a href='{verificationLink}' style='display:inline-block;background:#e65c00;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none;'>Verify Email</a></p>
                    <p style='word-break:break-all;font-size:12px;color:#666;'>{verificationLink}</p>
                    <p>This verification link expires in <strong>24 hours</strong>.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private static string HashOtp(string otp)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
            return Convert.ToBase64String(bytes);
        }
    }
}
