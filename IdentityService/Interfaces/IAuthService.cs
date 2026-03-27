using IdentityService.DTOs;

namespace IdentityService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<string?> SendOtpAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto);
        Task<IEnumerable<StoreDto>> GetActiveStoresAsync();
        Task<bool> CreateStoreAsync(StoreDto storeDto);
    }

    public record StoreDto(int Id, string StoreCode, string Name);
}
