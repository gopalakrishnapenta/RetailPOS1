using IdentityService.DTOs;

namespace IdentityService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginDto loginDto);
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<string?> SendOtpAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto);
    }

    public record StoreDto(int Id, string StoreCode, string Name);
}
