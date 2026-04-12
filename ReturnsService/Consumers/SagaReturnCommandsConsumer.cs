using MassTransit;
using RetailPOS.Contracts;
using ReturnsService.Data;
using Microsoft.EntityFrameworkCore;

namespace ReturnsService.Consumers
{
    public class SagaReturnCommandsConsumer : IConsumer<FinalizeReturnCommand>
    {
        private readonly ReturnsDbContext _context;
        private readonly ILogger<SagaReturnCommandsConsumer> _logger;

        public SagaReturnCommandsConsumer(ReturnsDbContext context, ILogger<SagaReturnCommandsConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<FinalizeReturnCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Saga finalizing return {command.ReturnId} with status {command.NewStatus}");

            // Access DB directly (ignoring query filters for background work)
            var returnRecord = await _context.Returns.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == command.ReturnId);
            if (returnRecord != null)
            {
                returnRecord.Status = command.NewStatus;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Return {command.ReturnId} status updated to {command.NewStatus} by Saga.");
            }
        }
    }
}
