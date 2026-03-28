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

            if (!user.IsEmailVerified)
                return new AuthResult { Success = false, Message = "EMAIL_NOT_VERIFIED" };

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
                var otp = new Random().Next(100000, 999999).ToString();
                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = registerDto.Password,
                    Role = registerDto.Role,
                    PrimaryStoreId = registerDto.StoreId,
                    EmployeeCode = $"E{new Random().Next(100, 999)}",
                    IsEmailVerified = false,
                    VerificationOtp = otp,
                    VerificationOtpExpiry = DateTime.UtcNow.AddMinutes(5)
                };
                
                _logger.LogInformation($"Adding user {user.Email} to repository with OTP: {otp}");
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // Send Email
                var subject = "NexusPOS Email Verification";
                var body = $"<h3>Verify Your Email</h3><p>Your verification code is: <b style='font-size: 24px;'>{otp}</b></p><p>This code expires in 5 minutes.</p>";
                await _emailService.SendEmailAsync(user.Email, subject, body);

                _logger.LogInformation("Registration successful! Verification OTP sent.");
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, $"Registration failed for {registerDto.Email}");
                throw;
            }
        }

        public async Task<bool> VerifyEmailAsync(string email, string otp)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null || user.VerificationOtp != otp || user.VerificationOtpExpiry < DateTime.UtcNow)
                return false;

            user.IsEmailVerified = true;
            user.VerificationOtp = null;
            user.VerificationOtpExpiry = null;
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

            _logger.LogInformation($"Resent Verification OTP for {email}: {otp}");
            
            var subject = "NexusPOS Verification Code (Resent)";
            var body = $"<h3>Verify Your Email</h3><p>Your new verification code is: <b style='font-size: 24px;'>{otp}</b></p>";
            await _emailService.SendEmailAsync(user.Email, subject, body);
            
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

            _logger.LogInformation($"Forgot Password OTP for {email}: {otp}");

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
            user.IsEmailVerified = true; // Resetting password successfully also verifies the email
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResult> GoogleLoginAsync(string idToken, int? storeId = null, string? role = null)
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
                    if (!storeId.HasValue || string.IsNullOrEmpty(role))
                    {
                        return new AuthResult { Success = false, Message = "GOOGLE_SIGNUP_REQUIRED_FIELDS" };
                    }

                    // Create new user for first-time Google sign-in
                    user = new User
                    {
                        Email = email,
                        Role = role, 
                        PrimaryStoreId = storeId.Value,
                        EmployeeCode = $"G{new Random().Next(100, 999)}",
                        PasswordHash = Guid.NewGuid().ToString(), // Dummy password
                        IsEmailVerified = true // Google emails are already verified
                    };
                    await _userRepository.AddAsync(user);
                    await _userRepository.SaveChangesAsync();
                    _logger.LogInformation($"Created new user {email} via Google signup with Role: {role}, Store: {storeId}");
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

        public async Task<bool> TestEmailAsync(string toEmail)
        {
            _logger.LogInformation($"[SMTP TEST] Initiating test email to {toEmail}");
            var subject = "NexusPOS SMTP Test Connection";
            var body = "<h3>Test Connection Successful</h3><p>Your SMTP settings are correctly configured and the NexusPOS Identity Service can send emails.</p>";
            
            try 
            {
                await _emailService.SendEmailAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SMTP TEST] Test email to {toEmail} failed.");
                throw; // Rethrow to let the controller handle the message
            }
        }
    }
}
