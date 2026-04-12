using MassTransit;
using RetailPOS.Contracts;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Consumers
{
    public class SagaUserOnboardingConsumer : IConsumer<CreateStaffProfileCommand>
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<SagaUserOnboardingConsumer> _logger;

        public SagaUserOnboardingConsumer(AdminDbContext context, ILogger<SagaUserOnboardingConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CreateStaffProfileCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Saga initiating profile creation for {command.Email}");

            try
            {
                var staff = await _context.StaffMembers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == command.UserId);
                
                if (staff == null)
                {
                    staff = new StaffMember
                    {
                        UserId = command.UserId,
                        Email = command.Email,
                        RegisteredDate = DateTime.UtcNow
                    };
                    _context.StaffMembers.Add(staff);
                }

                if (!string.IsNullOrEmpty(command.FullName)) staff.FullName = command.FullName;
                if (command.StoreId.HasValue) staff.AssignedStoreId = command.StoreId;
                if (!string.IsNullOrEmpty(command.RoleName)) 
                {
                    staff.AssignedRole = command.RoleName;
                    staff.IsAssigned = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Profile created/updated for {command.Email}. Publishing completion event.");
                
                await context.Publish<StaffProfileCreatedEvent>(new { UserId = command.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create profile for {command.Email}");
                // In a true bulletproof saga, we'd have a Failure event here too.
            }
        }
    }
}
