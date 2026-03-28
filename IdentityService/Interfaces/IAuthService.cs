using IdentityService.DTOs;

namespace IdentityService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginDto loginDto);
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<bool> VerifyEmailAsync(string email, string otp);
        Task<string?> ResendVerificationOtpAsync(string email);
        Task<string?> SendOtpAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto);
        Task<AuthResult> GoogleLoginAsync(string idToken, int? storeId = null, string? role = null);
        Task<List<StoreDto>> GetStoresAsync();
        Task<bool> TestEmailAsync(string toEmail);
    }

    public record StoreDto(int Id, string StoreCode, string Name);
}
