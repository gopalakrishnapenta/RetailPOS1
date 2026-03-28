using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.DTOs;
using IdentityService.Interfaces;
using Google.Apis.Auth;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IStoreRepository storeRepository, IConfiguration config, ILogger<AuthService> logger, IEmailService emailService)
        {
            _userRepository = userRepository;
            _storeRepository = storeRepository;
            _config = config;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<AuthResult> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || user.PasswordHash != loginDto.Password)
                return new AuthResult { Success = false, Message = "Invalid email or password" };

            // Ensure our primary admins have the correct role regardless of DB state
            if (user.Email.ToLower() == "admin@nexus.com" || user.Email.ToLower() == "admin@gmail.com") user.Role = "Admin";

            int currentStoreId = user.PrimaryStoreId;
            string? effectiveStoreCode = loginDto.StoreCode;

            if (user.Role == "Admin")
            {
                currentStoreId = 0; // Global context for Admin
            }
            else if (string.IsNullOrEmpty(effectiveStoreCode))
            {
                var store = await _storeRepository.GetByIdAsync(user.PrimaryStoreId);
                effectiveStoreCode = store?.StoreCode;
            }
            else
            {
                var store = await _storeRepository.SingleOrDefaultAsync(s => s.StoreCode == effectiveStoreCode);
                if (store == null || store.Id != user.PrimaryStoreId)
                {
                    return new AuthResult { Success = false, Message = "Your details are not matching with the selected Store ID." };
                }
                currentStoreId = store.Id;
            }

            var token = GenerateJwt(user, currentStoreId, effectiveStoreCode, loginDto.ShiftDate ?? DateTime.Today.ToString("yyyy-MM-dd"));
            return new AuthResult 
            { 
                Success = true, 
                Data = new AuthResponseDto { 
                    Token = token, 
                    Role = user.Role, 
                    Email = user.Email, 
                    StoreId = currentStoreId,
                    StoreCode = effectiveStoreCode
                } 
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

            if (registerDto.Role == "Admin")
            {
                _logger.LogWarning($"Registration rejected: Admin role cannot be created via signup for {registerDto.Email}");
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

            var subject = "Password Reset OTP";
            var body = $@"
                <h3>Password Reset Request</h3>
                <p>Hello,</p>
                <p>You requested a password reset for your account. Please use the following One-Time Password (OTP) to reset your password:</p>
                <h2 style='color: #4A90E2;'>{otp}</h2>
                <p>This OTP is valid for 10 minutes.</p>
                <p>If you did not request this, please ignore this email.</p>
                <br/>
                <p>Regards,<br/>RetailPOS Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);
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

        public async Task<AuthResult> GoogleLoginAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _config["Google:ClientId"]! }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                var email = payload.Email;

                var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Create new user for first-time Google sign-in
                    user = new User
                    {
                        Email = email,
                        Role = "Cashier", // Default role for new Google signups
                        PrimaryStoreId = 1, // Default store for simplicity
                        EmployeeCode = $"G{new Random().Next(100, 999)}",
                        PasswordHash = Guid.NewGuid().ToString() // Dummy password for Google users
                    };
                    await _userRepository.AddAsync(user);
                    await _userRepository.SaveChangesAsync();
                    _logger.LogInformation($"Created new user {email} via Google login.");
                }

                var store = await _storeRepository.GetByIdAsync(user.PrimaryStoreId);
                var token = GenerateJwt(user, user.PrimaryStoreId, store?.StoreCode, DateTime.Today.ToString("yyyy-MM-dd"));
                return new AuthResult 
                { 
                    Success = true, 
                    Data = new AuthResponseDto { 
                        Token = token, 
                        Role = user.Role, 
                        Email = user.Email, 
                        StoreId = user.PrimaryStoreId,
                        StoreCode = store?.StoreCode
                    } 
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Invalid Google ID Token.");
                return new AuthResult { Success = false, Message = "Invalid Google Token" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed.");
                return new AuthResult { Success = false, Message = "Google login error" };
            }
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
        public async Task<List<StoreDto>> GetStoresAsync()
        {
            var stores = await _storeRepository.GetAllAsync();
            return stores
                .Select(s => new StoreDto(s.Id, s.StoreCode, s.Name))
                .ToList();
        }
    }
}
