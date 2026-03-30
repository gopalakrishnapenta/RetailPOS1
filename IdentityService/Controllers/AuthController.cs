using Microsoft.AspNetCore.Mvc;
using IdentityService.Interfaces;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Authorization;


namespace IdentityService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success) return Unauthorized(new { message = result.Message });
            return Ok(new { requiresOtp = result.Message == "OTP_SENT", message = result.Message, data = result.Data });
        }

        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpDto request)
        {
            var result = await _authService.VerifyLoginOtpAsync(request);
            if (!result.Success) return Unauthorized(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            await _authService.RegisterAsync(request);
            return Ok(new { message = "Registration successful. Please check your email for the verification code." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            await _authService.VerifyEmailAsync(request.Email, request.Otp);
            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
        {
            await _authService.ResendVerificationOtpAsync(request.Email);
            return Ok(new { message = "Verification code resent successfully." });
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] ForgotPasswordDto request)
        {
            await _authService.SendOtpAsync(request.Email);
            return Ok(new { message = "If the email exists, an OTP was sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            var result = await _authService.GoogleLoginAsync(request.IdToken, request.StoreId, request.Role);
            if (!result.Success)
            {
                if (result.Message == "GOOGLE_SIGNUP_REQUIRED_FIELDS")
                {
                    return BadRequest(new { code = "GOOGLE_SIGNUP_REQUIRED_FIELDS", message = "Please select a Store and Role before signing up with Google." });
                }
                return Unauthorized(new { message = result.Message });
            }

            return Ok(result.Data);
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetStores()
        {
            var stores = await _authService.GetStoresAsync();
            return Ok(stores);
        }
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string email)
        {
            await _authService.TestEmailAsync(email);
            return Ok(new { message = $"Test email sent successfully to {email}" });
        }
    }
}
