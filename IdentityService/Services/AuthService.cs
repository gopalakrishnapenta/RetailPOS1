using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.DTOs;
using IdentityService.Interfaces;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IStoreRepository storeRepository, IConfiguration config, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _storeRepository = storeRepository;
            _config = config;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || user.PasswordHash != loginDto.Password)
                return new AuthResult { Success = false, Message = "Invalid email or password" };

            // Ensure our primary admins have the correct role regardless of DB state
            if (user.Email.ToLower() == "admin@nexus.com" || user.Email.ToLower() == "admin@gmail.com") user.Role = "Admin";

            int currentStoreId = user.PrimaryStoreId;

            if (user.Role == "Admin")
            {
                currentStoreId = 0; // Global context for Admin
            }
            else if (!string.IsNullOrEmpty(loginDto.StoreCode))
            {
                var store = await _storeRepository.SingleOrDefaultAsync(s => s.StoreCode == loginDto.StoreCode);
                if (store == null || store.Id != user.PrimaryStoreId)
                {
                    return new AuthResult { Success = false, Message = "Your details are not matching with the selected Store ID." };
                }
                currentStoreId = store.Id;
            }

            var token = GenerateJwt(user, currentStoreId, loginDto.StoreCode, loginDto.ShiftDate);
            return new AuthResult 
            { 
                Success = true, 
                Data = new AuthResponseDto { Token = token, Role = user.Role, Email = user.Email, StoreId = currentStoreId } 
            };
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation($"Registering user: {registerDto.Email} for Store: {registerDto.StoreId}");
            if (await _userRepository.AnyAsync(u => u.Email == registerDto.Email))
            {
                _logger.LogWarning($"Registration failed: Email {registerDto.Email} already exists.");
                return false;
            }

            try {
                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = registerDto.Password,
                    Role = registerDto.Role,
                    PrimaryStoreId = registerDto.StoreId,
                    EmployeeCode = $"E{new Random().Next(100, 999)}" // Auto-generate employee code if missing
                };
                
                _logger.LogInformation($"Adding user {user.Email} to repository...");
                await _userRepository.AddAsync(user);
                _logger.LogInformation($"Saving changes to database...");
                await _userRepository.SaveChangesAsync();
                _logger.LogInformation("Registration successful!");
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, $"Registration failed with exception for {registerDto.Email}");
                throw; // Rethrow to be caught by Controller
            }
        }

        public async Task<string?> SendOtpAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            var otp = new Random().Next(100000, 999999).ToString();
            user.Otp = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.SaveChangesAsync();

            _logger.LogWarning($"\n=== GMAIL SMTP MOCK ===\nTo: {user.Email}\nSubject: Password Reset OTP\nBody: Your OTP is {otp}\n=======================\n");
            return otp;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == resetDto.Email);
            if (user == null || user.Otp != resetDto.Otp || user.OtpExpiry < DateTime.UtcNow)
                return false;

            user.PasswordHash = resetDto.NewPassword;
            user.Otp = null;
            user.OtpExpiry = null;
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StoreDto>> GetActiveStoresAsync()
        {
            var stores = await _storeRepository.FindAsync(s => s.IsActive);
            return stores.Select(s => new StoreDto(s.Id, s.StoreCode, s.Name));
        }

        public async Task<bool> CreateStoreAsync(StoreDto storeDto)
        {
            if (await _storeRepository.AnyAsync(s => s.StoreCode == storeDto.StoreCode))
                return false;

            var store = new Store { StoreCode = storeDto.StoreCode, Name = storeDto.Name, IsActive = true };
            await _storeRepository.AddAsync(store);
            await _storeRepository.SaveChangesAsync();
            return true;
        }

        private string GenerateJwt(User user, int storeId, string? storeCode = null, string? shiftDate = null)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("StoreId", storeId.ToString())
            };

            if (!string.IsNullOrEmpty(storeCode)) claims.Add(new Claim("StoreCode", storeCode));
            if (!string.IsNullOrEmpty(shiftDate)) claims.Add(new Claim("ShiftDate", shiftDate));

            var token = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Audience"], claims, expires: DateTime.Now.AddHours(8), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
