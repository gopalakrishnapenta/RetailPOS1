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
using IdentityService.Exceptions;
using System.Linq;
using IdentityService.Repositories;

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
                throw new ValidationException("Invalid email or password.");

            if (!user.IsEmailVerified)
                throw new ValidationException("EMAIL_NOT_VERIFIED");

            var otp = new Random().Next(100000, 999999).ToString();
            user.Otp = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.SaveChangesAsync();

            var testAccounts = _config.GetSection("TestAccounts").Get<string[]>() ?? Array.Empty<string>();
            if (testAccounts.Any(a => a.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation($"[TEST OTP] Login OTP for {user.Email}: {otp}");
            }
            else
            {
                await _emailService.SendEmailAsync(user.Email, "NexusPOS Login", $"Code: {otp}");
            }

            return new AuthResult { Success = true, Message = "OTP_SENT" };
        }

        public async Task<AuthResult> VerifyLoginOtpAsync(VerifyLoginOtpDto verifyDto)
        {
            var user = await _userRepository.GetWithRolesByEmailAsync(verifyDto.Email);
            if (user == null) throw new NotFoundException("User not found.");

            if (user.Otp != verifyDto.Otp || user.OtpExpiry < DateTime.UtcNow)
                throw new ValidationException("Invalid or expired OTP.");

            user.Otp = null;
            user.OtpExpiry = null;
            await _userRepository.SaveChangesAsync();

            IdentityService.Models.UserStoreRole? mapping = null;
            if (!string.IsNullOrEmpty(verifyDto.StoreCode))
            {
                mapping = user.UserRoles.FirstOrDefault(ur => ur.Store?.StoreCode == verifyDto.StoreCode);
                if (mapping == null && user.UserRoles.Any(ur => ur.StoreId == null && ur.Role.Name == "Admin"))
                {
                    mapping = user.UserRoles.First(ur => ur.StoreId == null);
                }
            }
            else
            {
                mapping = user.UserRoles.OrderBy(ur => ur.StoreId == null ? 0 : 1).FirstOrDefault();
            }

            if (mapping == null) throw new ValidationException("Not authorized for this context.");

            var perms = mapping.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
            var token = GenerateJwt(user, mapping.Role.Name, perms, mapping.StoreId ?? 0, mapping.Store?.StoreCode, verifyDto.ShiftDate ?? DateTime.Today.ToString("yyyy-MM-dd"));
            
            return new AuthResult 
            { 
                Success = true, 
                Data = new AuthResponseDto { 
                    Token = token, 
                    Role = mapping.Role.Name, 
                    Email = user.Email, 
                    StoreId = mapping.StoreId ?? 0,
                    StoreCode = mapping.Store?.StoreCode,
                    Permissions = perms
                } 
            };
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            if (await _userRepository.AnyAsync(u => u.Email == registerDto.Email))
                throw new ConflictException("Account exists.");

            var otp = new Random().Next(100000, 999999).ToString();
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = registerDto.Password,
                EmployeeCode = $"E{new Random().Next(100, 999)}",
                VerificationOtp = otp,
                VerificationOtpExpiry = DateTime.UtcNow.AddMinutes(5)
            };

            var db = ((UserRepository)_userRepository).GetContext();
            var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == registerDto.Role);
            
            user.UserRoles.Add(new UserStoreRole { RoleId = roleEntity?.Id ?? 3, StoreId = registerDto.StoreId != 0 ? registerDto.StoreId : null });

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerifyEmailAsync(string email, string otp)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null || user.VerificationOtp != otp) return false;
            user.IsEmailVerified = true;
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<string?> ResendVerificationOtpAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;
            var otp = new Random().Next(100000, 999999).ToString();
            user.VerificationOtp = otp;
            user.VerificationOtpExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userRepository.SaveChangesAsync();
            return otp;
        }

        public async Task<string?> SendOtpAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;
            var otp = new Random().Next(100000, 999999).ToString();
            user.Otp = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userRepository.SaveChangesAsync();
            return otp;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == resetDto.Email);
            if (user == null || user.Otp != resetDto.Otp) return false;
            user.PasswordHash = resetDto.NewPassword;
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResult> GoogleLoginAsync(string idToken, int? storeId = null, string? role = null)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = new List<string> { _config["Google:ClientId"]! } });
            var user = await _userRepository.GetWithRolesByEmailAsync(payload.Email);
            
            if (user == null)
            {
                if (!storeId.HasValue || string.IsNullOrEmpty(role)) return new AuthResult { Success = false, Message = "SIGNUP_REQUIRED" };
                var db = ((UserRepository)_userRepository).GetContext();
                var roleEnt = await db.Roles.FirstOrDefaultAsync(r => r.Name == role);
                user = new IdentityService.Models.User { Email = payload.Email, PasswordHash = Guid.NewGuid().ToString(), IsEmailVerified = true };
                user.UserRoles.Add(new UserStoreRole { RoleId = roleEnt?.Id ?? 3, StoreId = storeId });
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
                user = await _userRepository.GetWithRolesByEmailAsync(payload.Email);
            }

            var map = user!.UserRoles.OrderBy(ur => ur.StoreId == null ? 0 : 1).FirstOrDefault();
            var perms = map!.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
            var token = GenerateJwt(user, map.Role.Name, perms, map.StoreId ?? 0, map.Store?.StoreCode);

            return new AuthResult { Success = true, Data = new AuthResponseDto { Token = token, Role = map.Role.Name, Email = user.Email, StoreId = map.StoreId ?? 0, Permissions = perms } };
        }

        private string GenerateJwt(IdentityService.Models.User user, string roleName, List<string> permissions, int storeId, string? storeCode = null, string? shiftDate = null)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("StoreId", storeId.ToString())
            };
            foreach (var p in permissions) claims.Add(new Claim("permission", p));
            if (storeCode != null) claims.Add(new Claim("StoreCode", storeCode));
            if (shiftDate != null) claims.Add(new Claim("ShiftDate", shiftDate));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system"));
            var token = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Audience"], claims, expires: DateTime.Now.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<List<StoreDto>> GetStoresAsync()
        {
            var stores = await _storeRepository.GetAllAsync();
            return stores.Select(s => new StoreDto(s.Id, s.StoreCode, s.Name)).ToList();
        }

        public async Task<bool> TestEmailAsync(string toEmail)
        {
            await _emailService.SendEmailAsync(toEmail, "Test", "Success");
            return true;
        }
    }
}
