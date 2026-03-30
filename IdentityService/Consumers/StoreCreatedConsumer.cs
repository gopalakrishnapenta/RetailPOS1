using MassTransit;
using RetailPOS.Contracts;
using IdentityService.Models;
using IdentityService.Interfaces;

namespace IdentityService.Consumers
{
    public class StoreCreatedConsumer : IConsumer<StoreCreatedEvent>, IConsumer<StoreUpdatedEvent>, IConsumer<StoreDeletedEvent>
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
                    StoreCode = data.StoreCode,
                    Name = data.Name,
                    IsActive = data.IsActive,
                    Location = data.Location
                };
                await _storeRepository.AddAsync(store);
                await _storeRepository.SaveChangesAsync();
                _logger.LogInformation($"Store {data.StoreCode} synced successfully in IdentityService.");
            }
        }

        public async Task Consume(ConsumeContext<StoreUpdatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StoreUpdatedEvent: {data.StoreCode} -> {data.Name}");

            var store = await _storeRepository.SingleOrDefaultAsync(s => s.StoreCode == data.StoreCode);
            if (store != null)
            {
                store.Name = data.Name;
                store.Location = data.Location;
                store.IsActive = data.IsActive;
                _storeRepository.Update(store);
                await _storeRepository.SaveChangesAsync();
                _logger.LogInformation($"Store {data.StoreCode} updated successfully in IdentityService.");
            }
        }

        public async Task Consume(ConsumeContext<StoreDeletedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StoreDeletedEvent: {data.StoreCode}");

            var store = await _storeRepository.SingleOrDefaultAsync(s => s.StoreCode == data.StoreCode);
            if (store != null)
            {
                store.IsActive = false; // Soft delete in Identity for login safety
                _storeRepository.Update(store);
                await _storeRepository.SaveChangesAsync();
                _logger.LogInformation($"Store {data.StoreCode} deactivated in IdentityService.");
            }
        }
    }
}
