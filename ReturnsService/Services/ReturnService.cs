using Microsoft.EntityFrameworkCore;
using ReturnsService.Data;
using ReturnsService.Models;
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

        public async Task<Return> InitiateReturnAsync(Return returnRequest)
        {
            returnRequest.Status = "Initiated";
            returnRequest.Date = DateTime.UtcNow;
            returnRequest.StoreId = _tenantProvider.StoreId;
            
            await _returnRepository.AddAsync(returnRequest);
            await _returnRepository.SaveChangesAsync();

            // Notify OrdersService that a return has been initiated
            await _publishEndpoint.Publish<ReturnInitiatedEvent>(new
            {
                OrderId = returnRequest.OriginalBillId,
                ServiceReturnId = returnRequest.Id,
                StoreId = returnRequest.StoreId,
                CustomerMobile = returnRequest.CustomerMobile,
                Items = new[] { new { ProductId = returnRequest.ProductId, Quantity = returnRequest.Quantity } }
            });

            return returnRequest;
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
