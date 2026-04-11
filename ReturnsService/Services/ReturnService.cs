using Microsoft.EntityFrameworkCore;
using ReturnsService.Data;
using ReturnsService.Models;
using ReturnsService.DTOs;
using ReturnsService.Interfaces;
using MassTransit;
using RetailPOS.Contracts;

namespace ReturnsService.Services
{
    public class ReturnService : IReturnService
    {
        private readonly IReturnRepository _returnRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ITenantProvider _tenantProvider;

        public ReturnService(IReturnRepository returnRepository, IPublishEndpoint publishEndpoint, ITenantProvider tenantProvider)
        {
            _returnRepository = returnRepository;
            _publishEndpoint = publishEndpoint;
            _tenantProvider = tenantProvider;
        }

        public async Task<IEnumerable<Return>> GetAllReturnsAsync()
        {
            return await _returnRepository.GetQueryable().OrderByDescending(r => r.Date).ToListAsync();
        }

        public async Task<Return> InitiateReturnAsync(ReturnInitiationDto returnDto)
        {
            var createdReturns = new List<Return>();

            foreach (var item in returnDto.Items)
            {
                var r = new Return
                {
                    OriginalBillId = returnDto.BillId,
                    Reason = returnDto.Reason,
                    Status = "Initiated",
                    Date = DateTime.UtcNow,
                    StoreId = _tenantProvider.StoreId,
                    ProductId = item.BillItemId, // In a more complex system, map BillItemId -> ProductId
                    Quantity = item.Quantity,
                    RefundAmount = item.RefundAmount,
                    CustomerMobile = returnDto.CustomerMobile
                };

                await _returnRepository.AddAsync(r);
                createdReturns.Add(r);
            }

            await _returnRepository.SaveChangesAsync();

            // Notify OrdersService for EACH item return initiated
            foreach (var r in createdReturns)
            {
                await _publishEndpoint.Publish<ReturnInitiatedEvent>(new
                {
                    OrderId = r.OriginalBillId,
                    ServiceReturnId = r.Id,
                    StoreId = r.StoreId,
                    Items = new[] { new { ProductId = r.ProductId, Quantity = r.Quantity } }
                });
            }

            return createdReturns.FirstOrDefault() ?? new Return();
        }

        public async Task ApproveReturnAsync(int id, string? note)
        {
            var r = await _returnRepository.GetByIdAsync(id);
            if (r == null) throw new KeyNotFoundException("Return request not found.");

            r.Status = "Approved";
            r.ManagerApprovalNote = note ?? string.Empty;

            await _returnRepository.SaveChangesAsync();

            // Publish OrderReturnedEvent for Catalog restocking and Order status update
            await _publishEndpoint.Publish<OrderReturnedEvent>(new
            {
                OrderId = r.OriginalBillId,
                ReturnId = r.Id,
                StoreId = r.StoreId,
                RefundAmount = r.RefundAmount, 
                CustomerMobile = r.CustomerMobile,
                Items = new[] { new { ProductId = r.ProductId, Quantity = r.Quantity } }
            });
        }

        public async Task RejectReturnAsync(int id, string? note)
        {
            var r = await _returnRepository.GetByIdAsync(id);
            if (r == null) throw new KeyNotFoundException("Return request not found.");

            r.Status = "Rejected";
            r.ManagerApprovalNote = note ?? string.Empty;

            await _returnRepository.SaveChangesAsync();
        }
    }
}
