using MassTransit;
using RetailPOS.Contracts;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(AdminDbContext context, ILogger<UserRegisteredConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming UserRegisteredEvent for User {data.Email}");

            try
            {
                // Global check: Ignore query filters to see if they are already in the system
                var staff = await _context.StaffMembers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == data.UserId);
                
                bool isNew = false;
                if (staff == null)
                {
                    isNew = true;
                    staff = new StaffMember
                    {
                        UserId = data.UserId,
                        Email = data.Email,
                        RegisteredDate = DateTime.UtcNow
                    };
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(data.FullName)) staff.FullName = data.FullName;
                if (data.StoreId.HasValue) staff.AssignedStoreId = data.StoreId;
                if (!string.IsNullOrEmpty(data.RoleName)) 
                {
                    staff.AssignedRole = data.RoleName;
                    staff.IsAssigned = true; // Mark as assigned if role exists
                }

                if (isNew)
                {
                    _context.StaffMembers.Add(staff);
                    _logger.LogInformation($"Added New Staff Member: {data.Email}");
                }
                else
                {
                    _logger.LogInformation($"Updated Existing Staff Member: {data.Email} (Assigned: {staff.IsAssigned})");
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process UserRegisteredEvent for {data.Email}");
            }
        }
    }
}
