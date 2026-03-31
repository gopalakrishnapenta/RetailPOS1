using IdentityService.Models;

namespace IdentityService.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetWithRolesByEmailAsync(string email);
        Task<User?> GetWithRolesByIdAsync(int id);
    }

    public interface IStoreRepository : IGenericRepository<Store>
    {
    }
}
