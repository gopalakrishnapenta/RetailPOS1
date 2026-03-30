using ReturnsService.Data;
using ReturnsService.Models;
using ReturnsService.Interfaces;

namespace ReturnsService.Repositories
{
    public class ReturnRepository : GenericRepository<Return>, IReturnRepository
    {
        public ReturnRepository(ReturnsDbContext context) : base(context)
        {
        }
    }
}
