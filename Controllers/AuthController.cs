using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantApp.Data;
using RestaurantApp.DTOs;
using RestaurantApp.Exceptions;
using RestaurantApp.Services;

namespace RestaurantApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly AppDbContext _context;

        public AuthController(IUserService userService, IAuthService authService, IOtpService otpService, AppDbContext context)
        {
            _userService = userService;
            _authService = authService;
            _otpService = otpService;
            _context = context;
        }

        /// <summary>Register a new customer account. An OTP is sent to the email for verification.</summary>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.RegisterUser(dto);

            var otpCode = await _otpService.GenerateOtpCode();
            await _otpService.SaveOtpCode(dto.Email, otpCode, "EmailVerification");
            var verificationToken = await _otpService.GenerateVerificationToken();
            await _otpService.SaveOtpCode(dto.Email, verificationToken, "EmailVerificationLink", TimeSpan.FromDays(1));

            var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email-link?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(verificationToken)}";
            await _otpService.SendVerificationEmail(dto.Email, otpCode, verificationLink);

            return StatusCode(201, new { message = "Registration successful. Check your email for the verification code.", user });
        }

        /// <summary>Login with email and password. Returns a JWT and Refresh token.</summary>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.ValidateUser(dto.Email, dto.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            if (!user.IsVerified)
                return StatusCode(403, new { message = "Email not verified. Please verify your email before logging in." });

            var tokens = await _authService.GenerateTokens(user);
            return Ok(tokens);
        }

        /// <summary>Refresh an expired access token using a valid refresh token.</summary>
        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            try
            {
                var tokens = await _authService.RefreshToken(dto.RefreshToken);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>Revoke a refresh token (Logout).</summary>
        [HttpPost("revoke")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            await _authService.RevokeToken(dto.RefreshToken);
            return Ok(new { message = "Token revoked successfully." });
        }

        /// <summary>Verify email address using the OTP sent during registration.</summary>
        [HttpPost("verify-email")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _otpService.VerifyOtpCode(dto.Email, dto.OtpCode, "EmailVerification");
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired OTP." });

            var user = await _userService.GetUserByEmail(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        /// <summary>Verify email address via one-click token link sent to the user's inbox (valid for 24 hours).</summary>
        [HttpGet("verify-email-link")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> VerifyEmailLink([FromQuery] string email, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Email and token are required." });

            var isValid = await _otpService.VerifyOtpCode(email, token, "EmailVerificationLink");
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired verification link." });

            var user = await _userService.GetUserByEmail(email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        /// <summary>Resend a new OTP to the given email address.</summary>
        [HttpPost("resend-otp")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetUserByEmail(dto.Email);
            if (user == null)
                return Ok(new { message = "If that email exists, a new code has been sent." }); // Avoid email enumeration

            if (user.LastOtpSentAt.HasValue && (DateTime.UtcNow - user.LastOtpSentAt.Value).TotalSeconds < 60)
                return BadRequest(new { message = "Please wait 60 seconds before requesting a new code." });

            var otpCode = await _otpService.GenerateOtpCode();
            await _otpService.SaveOtpCode(dto.Email, otpCode, dto.Purpose ?? "EmailVerification");
            if ((dto.Purpose ?? "EmailVerification") == "EmailVerification")
            {
                var verificationToken = await _otpService.GenerateVerificationToken();
                await _otpService.SaveOtpCode(dto.Email, verificationToken, "EmailVerificationLink", TimeSpan.FromDays(1));
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email-link?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(verificationToken)}";
                await _otpService.SendVerificationEmail(dto.Email, otpCode, verificationLink);
            }
            else
            {
                await _otpService.SendOtp(dto.Email, otpCode);
            }

            user.LastOtpSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "A new verification code has been sent to your email." });
        }

        /// <summary>Request a password reset OTP.</summary>
        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetUserByEmail(dto.Email);
            if (user != null)
            {
                var otpCode = await _otpService.GenerateOtpCode();
                await _otpService.SaveOtpCode(dto.Email, otpCode, "PasswordReset");
                await _otpService.SendOtp(dto.Email, otpCode);

                user.LastOtpSentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Always return same response to avoid email enumeration
            return Ok(new { message = "If that email is registered, a reset code has been sent." });
        }

        /// <summary>Reset password using OTP from forgot-password flow.</summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _otpService.VerifyOtpCode(dto.Email, dto.OtpCode, "PasswordReset");
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired OTP." });

            var user = await _userService.GetUserByEmail(dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.PasswordHash = await _authService.HashPassword(dto.NewPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }
    }
}
