using OrdersService.DTOs;
using OrdersService.Models;

namespace OrdersService.Interfaces
{
    public interface IBillService
    {
        Task<IEnumerable<BillDto>> GetAllBillsAsync();
        Task<BillDto?> GetBillByIdAsync(int id);
        Task<BillDto> CreateOrUpdateCartAsync(BillDto cartDto);
        Task<bool> FinalizeBillAsync(int id);
        Task<bool> HoldBillAsync(int id);
    }

    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
        Task<CustomerDto?> GetByMobileAsync(string mobile);
        Task<CustomerDto> CreateOrUpdateCustomerAsync(CustomerDto customerDto);
    }
}
