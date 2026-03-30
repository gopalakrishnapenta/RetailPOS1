using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using OrdersService.Interfaces;

namespace OrdersService.Repositories
{
    public class BillRepository : GenericRepository<Bill>, IBillRepository
    {
        public BillRepository(OrdersDbContext context) : base(context)
        {
        }

        public async Task<Bill?> GetBillWithItemsAsync(int id)
        {
            return await _dbSet.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Bill>> GetAllBillsWithItemsAsync()
        {
            return await _dbSet.Include(b => b.Items).ToListAsync();
        }
    }

    public class BillItemRepository : GenericRepository<BillItem>, IBillItemRepository
    {
        public BillItemRepository(OrdersDbContext context) : base(context)
        {
        }
    }

    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(OrdersDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByMobileAsync(string mobile)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Mobile == mobile);
        }
    }
}
