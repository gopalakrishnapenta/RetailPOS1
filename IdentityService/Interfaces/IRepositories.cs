using IdentityService.Models;

namespace IdentityService.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetWithRolesByEmailAsync(string email);
    }

    public interface IStoreRepository : IGenericRepository<Store>
    {
    }
}
