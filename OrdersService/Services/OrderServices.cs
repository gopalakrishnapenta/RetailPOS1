using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using OrdersService.DTOs;
using OrdersService.Interfaces;
using MassTransit;
using RetailPOS.Contracts;
using OrdersService.Exceptions;

namespace OrdersService.Services
{
    public class BillService : IBillService
    {
        private readonly IBillRepository _billRepository;
        private readonly IBillItemRepository _billItemRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ITenantProvider _tenantProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public BillService(
            IBillRepository billRepository, 
            IBillItemRepository billItemRepository,
            ICustomerRepository customerRepository,
            ITenantProvider tenantProvider,
            IPublishEndpoint publishEndpoint)
        {
            _billRepository = billRepository;
            _billItemRepository = billItemRepository;
            _customerRepository = customerRepository;
            _tenantProvider = tenantProvider;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<IEnumerable<BillDto>> GetAllBillsAsync()
        {
            var bills = await _billRepository.GetAllBillsWithItemsAsync();
            return bills.Select(MapToDto);
        }

        public async Task<BillDto?> GetBillByIdAsync(int id)
        {
            var bill = await _billRepository.GetBillWithItemsAsync(id);
            if (bill == null) throw new NotFoundException($"Bill with ID {id} not found.");

            return MapToDto(bill);
        }

        public async Task<BillDto> CreateOrUpdateCartAsync(BillDto cartDto)
        {
            Bill bill;
            if (cartDto.Id == 0)
            {
                bill = new Bill { 
                    Status = "Draft", 
                    BillNumber = cartDto.BillNumber, 
                    CustomerMobile = cartDto.CustomerMobile,
                    CustomerName = cartDto.CustomerName,
                    TotalAmount = cartDto.TotalAmount,
                    TaxAmount = cartDto.TaxAmount,
                    StoreId = cartDto.StoreId != 0 ? cartDto.StoreId : _tenantProvider.StoreId,
                    CashierId = cartDto.CashierId != 0 ? cartDto.CashierId : _tenantProvider.UserId
                };
                await _billRepository.AddAsync(bill);
            }
            else
            {
                bill = await _billRepository.GetBillWithItemsAsync(cartDto.Id) 
                       ?? throw new NotFoundException($"Bill with ID {cartDto.Id} not found.");
                bill.CustomerMobile = cartDto.CustomerMobile;
                bill.CustomerName = cartDto.CustomerName;
                bill.TotalAmount = cartDto.TotalAmount;
                bill.TaxAmount = cartDto.TaxAmount;
                
                // Clear existing items for update
                _billItemRepository.DeleteRange(bill.Items);
                bill.Items.Clear();

                _billRepository.Update(bill);
            }

            // Add new items
            if (cartDto.Items != null)
            {
                foreach (var item in cartDto.Items)
                {
                    bill.Items.Add(new BillItem {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        SubTotal = item.SubTotal
                    });
                }
            }

            await _billRepository.SaveChangesAsync();
            return MapToDto(bill);
        }

        public async Task<bool> FinalizeBillAsync(int id)
        {
            var bill = await _billRepository.GetBillWithItemsAsync(id);
            if (bill == null) throw new NotFoundException($"Bill with ID {id} not found.");

            // We NO LONGER publish OrderPlacedEvent here!
            // We wait for the PaymentProcessedEvent from the Payment Service instead.
            bill.Status = "PendingPayment";
            await _billRepository.SaveChangesAsync();

            // TRIGGER SAGA
            await _publishEndpoint.Publish<CheckoutInitiatedEvent>(new
            {
                OrderId = bill.Id,
                StoreId = bill.StoreId,
                CashierId = bill.CashierId,
                TotalAmount = bill.TotalAmount,
                TaxAmount = bill.TaxAmount,
                Date = DateTime.UtcNow,
                CustomerMobile = bill.CustomerMobile,
                Items = bill.Items.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
            });

            return true;
        }

        public async Task<bool> HoldBillAsync(int id)
        {
            var bill = await _billRepository.GetByIdAsync(id);
            if (bill == null) throw new NotFoundException($"Bill with ID {id} not found.");

            bill.Status = "Held";
            await _billRepository.SaveChangesAsync();
            return true;
        }

        private BillDto MapToDto(Bill b) => new BillDto
        {
            Id = b.Id,
            BillNumber = b.BillNumber,
            Date = b.Date,
            CustomerMobile = b.CustomerMobile,
            CustomerName = b.CustomerName,
            TotalAmount = b.TotalAmount,
            TaxAmount = b.TaxAmount,
            Status = b.Status,
            StoreId = b.StoreId,
            CashierId = b.CashierId,
            Items = b.Items.Select(i => new BillItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            return customers.Select(c => new CustomerDto { Mobile = c.Mobile, Name = c.Name });
        }

        public async Task<CustomerDto?> GetByMobileAsync(string mobile)
        {
            var c = await _customerRepository.GetByMobileAsync(mobile);
            if (c == null) throw new NotFoundException($"Customer with mobile {mobile} not found.");
            return new CustomerDto { Mobile = c.Mobile, Name = c.Name };
        }

        public async Task<CustomerDto> CreateOrUpdateCustomerAsync(CustomerDto dto)
        {
            var existing = await _customerRepository.GetByMobileAsync(dto.Mobile);
            if (existing != null)
            {
                existing.Name = dto.Name;
                _customerRepository.Update(existing);
            }
            else
            {
                await _customerRepository.AddAsync(new Customer { Mobile = dto.Mobile, Name = dto.Name });
            }
            await _customerRepository.SaveChangesAsync();
            return dto;
        }
    }
}
