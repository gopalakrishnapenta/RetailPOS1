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
                var existing = await _context.StaffMembers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == data.UserId);
                if (existing != null) return;

                var staff = new StaffMember
                {
                    UserId = data.UserId,
                    Email = data.Email,
                    FullName = data.Email.Split('@')[0], // Default name from email
                    IsAssigned = false,
                    RegisteredDate = DateTime.UtcNow
                };

                _context.StaffMembers.Add(staff);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Added New Pending Staff Member: {data.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process UserRegisteredEvent for {data.Email}");
            }
        }
    }
}
