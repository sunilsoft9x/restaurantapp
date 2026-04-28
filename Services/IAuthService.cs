using RestaurantApp.Models;
using RestaurantApp.DTOs;

namespace RestaurantApp.Services
{
    public interface IAuthService
    {
        Task<string> GenerateJwtToken(UserModel user);
        Task<bool> VerifyPassword(string enteredPassword, string storedHash);
        Task<string> HashPassword(string password);
        Task<TokenResponseDto> GenerateTokens(UserModel user);
        Task<TokenResponseDto> RefreshToken(string refreshToken);
        Task RevokeToken(string refreshToken);
    }
}