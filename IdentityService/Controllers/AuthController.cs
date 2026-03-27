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
            if (result == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(result);
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

        [HttpGet("stores")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStores()
        {
            return Ok(await _authService.GetActiveStoresAsync());
        }

        [HttpPost("stores")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncStore([FromBody] StoreDto storeDto)
        {
            var success = await _authService.CreateStoreAsync(storeDto);
            return success ? Ok() : BadRequest();
        }
    }
}
