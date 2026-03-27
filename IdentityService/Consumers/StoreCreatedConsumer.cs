using MassTransit;
using RetailPOS.Contracts;
using IdentityService.Models;
using IdentityService.Interfaces;

namespace IdentityService.Consumers
{
    public class StoreCreatedConsumer : IConsumer<StoreCreatedEvent>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly ILogger<StoreCreatedConsumer> _logger;

        public StoreCreatedConsumer(IStoreRepository storeRepository, ILogger<StoreCreatedConsumer> logger)
        {
            _storeRepository = storeRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StoreCreatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StoreCreatedEvent: {data.Name} ({data.StoreCode})");

            // Check if store already exists (idempotency)
            var existing = await _storeRepository.SingleOrDefaultAsync(s => s.StoreCode == data.StoreCode);
            if (existing == null)
            {
                var store = new Store
                {
                    // We try to keep IDs in sync if possible, but IdentityService uses its own autoincrement.
                    // However, for consistency, we could try to set the ID if the DB allows it (SET IDENTITY_INSERT).
                    // For now, we'll let it autoincrement as long as StoreCode is the primary logical key.
                    StoreCode = data.StoreCode,
                    Name = data.Name,
                    IsActive = true,
                    Location = "Synced via Event"
                };
                await _storeRepository.AddAsync(store);
                await _storeRepository.SaveChangesAsync();
                _logger.LogInformation($"Store {data.StoreCode} synced successfully in IdentityService.");
            }
        }
    }
}
