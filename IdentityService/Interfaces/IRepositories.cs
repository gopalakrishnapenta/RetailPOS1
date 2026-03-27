using IdentityService.Models;

namespace IdentityService.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
    }

    public interface IStoreRepository : IGenericRepository<Store>
    {
    }
}
