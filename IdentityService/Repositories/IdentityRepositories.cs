using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public AppDbContext GetContext() => _context;

        public async Task<User?> GetWithRolesByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Store)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }

    public class StoreRepository : GenericRepository<Store>, IStoreRepository
    {
        public StoreRepository(AppDbContext context) : base(context)
        {
        }
    }
}
