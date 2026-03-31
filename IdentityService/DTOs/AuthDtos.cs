using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? StoreCode { get; set; }
        public string? ShiftDate { get; set; }
    }

    public class RegisterDto
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9.]+@gmail\.com$", ErrorMessage = "Only @gmail.com accounts are allowed.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must be at least 8 characters, include uppercase, lowercase, number, and symbol.")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public bool RequiresOtp { get; set; } = false;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string? StoreCode { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class VerifyLoginOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string? StoreCode { get; set; }
        public string? ShiftDate { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RefreshToken { get; set; }
        public AuthResponseDto? Data { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string? Role { get; set; }
    }

    public class VerifyEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResendVerificationDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class TokenRequestDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
