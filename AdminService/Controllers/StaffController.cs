using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;
using MassTransit;
using RetailPOS.Contracts;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Permissions.Admin.StoresManage)] // Only Admin/HR can manage staff
    public class StaffController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public StaffController(AdminDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffMember>>> GetStaff()
        {
            var query = _context.StaffMembers.AsQueryable();
            if (User.IsInRole("Admin"))
            {
                query = query.IgnoreQueryFilters();
            }
            return await query.ToListAsync();
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<StaffMember>>> GetPendingStaff()
        {
            return await _context.StaffMembers.Where(sm => !sm.IsAssigned).ToListAsync();
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignStaff([FromBody] StaffAssignmentRequest request)
        {
            var staff = await _context.StaffMembers.IgnoreQueryFilters().FirstOrDefaultAsync(sm => sm.UserId == request.UserId);
            if (staff == null) return NotFound("Staff member not found.");

            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == request.StoreId);
            if (store == null && request.StoreId != 0) return BadRequest("Target Store not found.");

            // Update local Admin Service record
            staff.AssignedStoreId = request.StoreId == 0 ? null : request.StoreId;
            staff.AssignedRole = request.Role;
            staff.IsAssigned = true;

            await _context.SaveChangesAsync();

            // Notify Identity Service to grant actual access
            await _publishEndpoint.Publish<StaffAssignedEvent>(new
            {
                UserId = staff.UserId,
                StoreId = request.StoreId,
                RoleName = request.Role
            });

            return Ok(new { message = $"Successfully assigned {staff.Email} to {store?.Name ?? "Global Admin"} as {request.Role}" });
        }
    }

    public class StaffAssignmentRequest
    {
        public int UserId { get; set; }
        public int StoreId { get; set; } // 0 for Global Admin
        public string Role { get; set; } = "Cashier";
    }
}
