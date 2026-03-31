using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.DTOs;
using IdentityService.Interfaces;
using IdentityService.Exceptions;
using IdentityService.Repositories;
using MassTransit;
using RetailPOS.Contracts;
using ModelsUser = IdentityService.Models.User;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthService(IUserRepository userRepository, IStoreRepository storeRepository, IConfiguration config, ILogger<AuthService> logger, IEmailService emailService, IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _storeRepository = storeRepository;
            _config = config;
            _logger = logger;
            _emailService = emailService;
            _publishEndpoint = publishEndpoint;
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

            if (mapping == null) 
            {
                _logger.LogWarning($"User {user.Email} has no Store Assignment. Returning PENDING_STAFF.");
                return new AuthResult 
                { 
                    Success = true, 
                    Data = new AuthResponseDto { 
                        Token = null, 
                        Role = "PENDING_STAFF", 
                        Email = user.Email, 
                        StoreId = 0,
                        Permissions = new List<string>()
                    } 
                };
            }

            var perms = mapping.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
            var token = GenerateJwt(user, mapping.Role.Name, perms, mapping.StoreId ?? 0, mapping.Store?.StoreCode, verifyDto.ShiftDate ?? DateTime.Today.ToString("yyyy-MM-dd"));
            
            // [PHASE 2] Generate and Save Refresh Token
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.SaveChangesAsync();

            return new AuthResult 
            { 
                Success = true, 
                RefreshToken = refreshToken,
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
            var user = new ModelsUser
            {
                Email = registerDto.Email,
                PasswordHash = registerDto.Password,
                EmployeeCode = $"E{new Random().Next(100, 999)}",
                VerificationOtp = otp,
                VerificationOtpExpiry = DateTime.UtcNow.AddMinutes(5)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Notify Admin Service that a new staff member registered
            await _publishEndpoint.Publish<UserRegisteredEvent>(new
            {
                UserId = user.Id,
                Email = user.Email
            });
            
            // Actually send the verification email!
            await _emailService.SendEmailAsync(user.Email, "NexusPOS - Verification Code", $"Your verification code is: {otp}");

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
            
            // Actually send the verification email!
            await _emailService.SendEmailAsync(user.Email, "NexusPOS - Verification Code (Resent)", $"Your verification code is: {otp}");
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
            
            // Actually send the password reset email!
            await _emailService.SendEmailAsync(user.Email, "NexusPOS - Password Reset Code", $"Your password reset code is: {otp}");
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
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings { Audience = new List<string> { _config["Google:ClientId"]! } });
            var user = await _userRepository.GetWithRolesByEmailAsync(payload.Email);
            
            if (user == null)
            {
                if (!storeId.HasValue || string.IsNullOrEmpty(role)) return new AuthResult { Success = false, Message = "SIGNUP_REQUIRED" };
                var db = ((UserRepository)_userRepository).GetContext();
                var roleEnt = await db.Roles.FirstOrDefaultAsync(r => r.Name == role);
                user = new ModelsUser { Email = payload.Email, PasswordHash = Guid.NewGuid().ToString(), IsEmailVerified = true };
                user.UserRoles.Add(new UserStoreRole { RoleId = roleEnt?.Id ?? 3, StoreId = storeId });
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
                user = await _userRepository.GetWithRolesByEmailAsync(payload.Email);
            }

            var map = user!.UserRoles.OrderBy(ur => ur.StoreId == null ? 0 : 1).FirstOrDefault();
            var perms = map!.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
            var token = GenerateJwt(user, map.Role.Name, perms, map.StoreId ?? 0, map.Store?.StoreCode);

            // [PHASE 2] Generate and Save Refresh Token
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.SaveChangesAsync();

            return new AuthResult { Success = true, RefreshToken = refreshToken, Data = new AuthResponseDto { Token = token, Role = map.Role.Name, Email = user.Email, StoreId = map.StoreId ?? 0, Permissions = perms } };
        }

        private string GenerateJwt(ModelsUser user, string roleName, List<string> permissions, int storeId, string? storeCode = null, string? shiftDate = null)
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
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            // Generate token with multiple audiences
            var audiences = _config.GetSection("Jwt:Audiences").Get<string[]>() ?? new[] { "RetailPOS_Services" };
            foreach (var aud in audiences)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
            }

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"], 
                null, // Null because we added audiences to the claims list
                claims, 
                expires: DateTime.Now.AddHours(1), // Reduced expiry for Access Token (standard with Refresh flow)
                signingCredentials: creds);
                
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // Mapping to all services
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super_secret_key_1234567890_pos_system")),
                ValidateLifetime = false // Here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<AuthResult> RefreshTokenAsync(TokenRequestDto tokenRequestDto)
        {
            var accessToken = tokenRequestDto.AccessToken;
            var refreshToken = tokenRequestDto.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                throw new ValidationException("Invalid token payload.");

            var user = await _userRepository.GetWithRolesByIdAsync(int.Parse(userIdStr));

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
                throw new ValidationException("Invalid refresh token.");

            // Get mapping for currently logged in store (from claims)
            var storeIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "StoreId")?.Value;
            int currentStoreId = int.Parse(storeIdClaim ?? "0");
            
            var mapping = user.UserRoles.FirstOrDefault(ur => ur.StoreId == currentStoreId);
            if (mapping == null) mapping = user.UserRoles.FirstOrDefault(); // Fallback

            if (mapping == null) throw new ValidationException("User has no valid roles for this store.");

            var perms = mapping.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
            var newAccessToken = GenerateJwt(user, mapping.Role.Name, perms, mapping.StoreId ?? 0, mapping.Store?.StoreCode);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userRepository.SaveChangesAsync();

            return new AuthResult
            {
                Success = true,
                RefreshToken = newRefreshToken,
                Data = new AuthResponseDto
                {
                    Token = newAccessToken,
                    Role = mapping.Role.Name,
                    Email = user.Email,
                    StoreId = mapping.StoreId ?? 0,
                    StoreCode = mapping.Store?.StoreCode,
                    Permissions = perms
                }
            };
        }

        public async Task<bool> LogoutAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userRepository.SaveChangesAsync();
            return true;
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
        public async Task<List<UserSyncDto>> GetUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users
                .Select(u => new UserSyncDto(u.Id, u.Email, u.IsEmailVerified))
                .ToList();
        }
    }
}
