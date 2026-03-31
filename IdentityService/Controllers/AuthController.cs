using Microsoft.AspNetCore.Mvc;
using IdentityService.Interfaces;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;


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
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (!result.Success) return Unauthorized(new { message = result.Message });
            return Ok(new { requiresOtp = result.Message == "OTP_SENT", message = result.Message, data = result.Data });
        }

        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpDto verifyDto)
        {
            var result = await _authService.VerifyLoginOtpAsync(verifyDto);
            if (!result.Success) return Unauthorized(new { message = result.Message });
            
            if (result.RefreshToken != null)
                SetTokenCookie(result.RefreshToken);

            return Ok(result.Data);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            await _authService.RegisterAsync(registerDto);
            return Ok(new { message = "Registration successful. Please check your email for the verification code." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto verifyDto)
        {
            await _authService.VerifyEmailAsync(verifyDto.Email, verifyDto.Otp);
            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto resendDto)
        {
            await _authService.ResendVerificationOtpAsync(resendDto.Email);
            return Ok(new { message = "Verification code resent successfully." });
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] ForgotPasswordDto forgotDto)
        {
            await _authService.SendOtpAsync(forgotDto.Email);
            return Ok(new { message = "If the email exists, an OTP was sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            await _authService.ResetPasswordAsync(resetDto);
            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto googleDto)
        {
            var result = await _authService.GoogleLoginAsync(googleDto.IdToken, googleDto.StoreId, googleDto.Role);
            if (!result.Success)
            {
                if (result.Message == "GOOGLE_SIGNUP_REQUIRED_FIELDS")
                {
                    return BadRequest(new { code = "GOOGLE_SIGNUP_REQUIRED_FIELDS", message = "Please select a Store and Role before signing up with Google." });
                }
                return Unauthorized(new { message = result.Message });
            }

            if (result.RefreshToken != null)
                SetTokenCookie(result.RefreshToken);

            return Ok(result.Data);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authService.GetUsersAsync();
            return Ok(users);
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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto tokenRequestDto)
        {
            // Extract refresh token from cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "Refresh token missing." });

            tokenRequestDto.RefreshToken = refreshToken;
            
            var result = await _authService.RefreshTokenAsync(tokenRequestDto);
            if (!result.Success) return Unauthorized(new { message = result.Message });

            if (result.RefreshToken != null)
                SetTokenCookie(result.RefreshToken);

            return Ok(result.Data);
        }

        [Authorize(Policy = Permissions.Auth.Logout)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            await _authService.LogoutAsync(email);
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logged out successfully" });
        }

        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true, // Ensure this is true in production (HTTPS)
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
