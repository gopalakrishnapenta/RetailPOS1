using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffSyncController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StaffSyncController> _logger;

        public StaffSyncController(AdminDbContext context, IHttpClientFactory httpClientFactory, ILogger<StaffSyncController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("re-sync")]
        public async Task<IActionResult> ReSync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("http://127.0.0.1:5001/api/Auth/users");
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, "Failed to fetch users from Identity Service.");

                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserSyncDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (users == null) return BadRequest("Invalid response from Identity Service.");

                int addedCount = 0;
                foreach (var user in users)
                {
                    var existing = await _context.StaffMembers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (existing == null)
                    {
                        var staff = new StaffMember
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            FullName = user.Email.Split('@')[0],
                            IsAssigned = false,
                            RegisteredDate = DateTime.UtcNow
                        };
                        _context.StaffMembers.Add(staff);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = $"Sync complete. Added {addedCount} missing staff members.", totalSynced = users.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during staff re-sync.");
                return StatusCode(500, $"Internal error during sync: {ex.Message}");
            }
        }
    }

    public record UserSyncDto(int Id, string Email, bool IsEmailVerified);
}
