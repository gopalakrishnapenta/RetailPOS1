using OrdersService.Models;

namespace OrdersService.Interfaces
{
    public interface IBillRepository : IGenericRepository<Bill>
    {
        Task<Bill?> GetBillWithItemsAsync(int id);
        Task<IEnumerable<Bill>> GetAllBillsWithItemsAsync();
    }

    public interface IBillItemRepository : IGenericRepository<BillItem>
    {
    }

    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<Customer?> GetByMobileAsync(string mobile);
    }
}
