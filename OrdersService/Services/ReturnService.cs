using OrdersService.Models;
using OrdersService.Interfaces;
using OrdersService.DTOs;
using MassTransit;
using RetailPOS.Contracts;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Services
{
    public class ReturnService : IReturnService
    {
        private readonly IReturnRepository _returnRepo;
        private readonly IBillRepository _billRepo;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<ReturnService> _logger;

        public ReturnService(
            IReturnRepository returnRepo, 
            IBillRepository billRepo,
            IPublishEndpoint publishEndpoint,
            ITenantProvider tenantProvider,
            ILogger<ReturnService> logger)
        {
            _returnRepo = returnRepo;
            _billRepo = billRepo;
            _publishEndpoint = publishEndpoint;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<Return> InitiateReturnAsync(Return returnRequest)
        {
            returnRequest.Status = "Initiated";
            await _returnRepo.AddAsync(returnRequest);
            await _returnRepo.SaveChangesAsync();
            _logger.LogInformation($"Return initiated for BillId: {returnRequest.OriginalBillId}, ProductId: {returnRequest.ProductId}");
            return returnRequest;
        }

        public async Task<bool> ApproveReturnAsync(int returnId, string? approvalNote)
        {
            var ret = await _returnRepo.GetByIdAsync(returnId);
            if (ret == null || ret.Status != "Initiated") return false;

            var bill = await _billRepo.GetBillWithItemsAsync(ret.OriginalBillId);
            if (bill == null) return false;

            var item = bill.Items.FirstOrDefault(i => i.ProductId == ret.ProductId);
            if (item == null) return false;

            // Update Return State
            ret.Status = "Approved";
            ret.ManagerApprovalNote = approvalNote ?? "Approved by manager";
            _returnRepo.Update(ret);

            // Publish Event to sync Stock and Dashboard
            decimal refundAmount = item.UnitPrice * ret.Quantity;

            await _publishEndpoint.Publish<OrderReturnedEvent>(new
            {
                OrderId = ret.OriginalBillId,
                ReturnId = ret.Id,
                StoreId = ret.StoreId != 0 ? ret.StoreId : _tenantProvider.StoreId,
                RefundAmount = refundAmount,
                Items = new[] { new { ProductId = ret.ProductId, Quantity = ret.Quantity } }
            });

            await _returnRepo.SaveChangesAsync();
            _logger.LogInformation($"Return {returnId} approved successfully for amount {refundAmount}");

            return true;
        }

        public async Task<bool> RejectReturnAsync(int returnId, string? rejectionNote)
        {
            var ret = await _returnRepo.GetByIdAsync(returnId);
            if (ret == null || ret.Status != "Initiated") return false;

            ret.Status = "Rejected";
            ret.ManagerApprovalNote = rejectionNote ?? "Rejected by manager";
            _returnRepo.Update(ret);
            await _returnRepo.SaveChangesAsync();
            _logger.LogInformation($"Return {returnId} rejected. Reason: {rejectionNote}");
            return true;
        }

        public async Task<IEnumerable<ReturnDetailedDto>> GetAllReturnsAsync()
        {
            var returns = await _returnRepo.GetAllAsync();
            var bills = await _billRepo.GetAllBillsWithItemsAsync();

            var result = from r in returns
                         join b in bills on r.OriginalBillId equals b.Id
                         let item = b.Items.FirstOrDefault(i => i.ProductId == r.ProductId)
                         select new ReturnDetailedDto
                         {
                             Id = r.Id,
                             OriginalBillId = r.OriginalBillId,
                             ProductName = item?.ProductName ?? "Unknown Product",
                             Quantity = r.Quantity,
                             Reason = r.Reason,
                             Status = r.Status,
                             CustomerName = b.CustomerName,
                             Date = r.Date
                         };

            return result;
        }
    }
}
