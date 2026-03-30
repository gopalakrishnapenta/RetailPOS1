using MassTransit;
using RetailPOS.Contracts;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Consumers
{
    public class StaffAssignedConsumer : IConsumer<StaffAssignedEvent>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StaffAssignedConsumer> _logger;

        public StaffAssignedConsumer(AppDbContext context, ILogger<StaffAssignedConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StaffAssignedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StaffAssignedEvent for User {data.UserId} at Store {data.StoreId}");

            try
            {
                var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == data.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User {data.UserId} not found for assignment.");
                    return;
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == data.RoleName);
                if (role == null)
                {
                    _logger.LogError($"Role {data.RoleName} not found in Identity database.");
                    return;
                }

                // Check if this specific store/role already exists to avoid duplicates
                var existing = user.UserRoles.FirstOrDefault(ur => ur.StoreId == data.StoreId && ur.RoleId == role.Id);
                if (existing != null)
                {
                    _logger.LogInformation($"Assignment already exists for User {data.UserId} at Store {data.StoreId}.");
                    return;
                }

                // Create the mapping!
                user.UserRoles.Add(new UserStoreRole
                {
                    UserId = data.UserId,
                    StoreId = data.StoreId != 0 ? data.StoreId : null,
                    RoleId = role.Id
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully assigned User {data.UserId} to Store {data.StoreId} as {data.RoleName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to assign staff member {data.UserId} to store {data.StoreId}");
            }
        }
    }
}
