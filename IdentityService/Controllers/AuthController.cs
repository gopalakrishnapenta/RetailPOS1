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
            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            try {
                var success = await _authService.RegisterAsync(request);
                if (!success)
                    return BadRequest(new { message = "Email already exists" });

                return Ok(new { message = "Registration successful" });
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"Registration failed: {ex.Message}", details = ex.InnerException?.Message });
            }
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
            var success = await _authService.ResetPasswordAsync(request);
            if (!success)
                return BadRequest(new { message = "Invalid or expired OTP" });

            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            var result = await _authService.GoogleLoginAsync(request.IdToken);
            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetStores()
        {
            var stores = await _authService.GetStoresAsync();
            return Ok(stores);
        }

    }
}
