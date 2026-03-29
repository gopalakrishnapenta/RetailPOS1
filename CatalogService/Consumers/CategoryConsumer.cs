using MassTransit;
using RetailPOS.Contracts;
using CatalogService.Models;
using CatalogService.Interfaces;

namespace CatalogService.Consumers
{
    public class CategoryConsumer : IConsumer<CategoryCreatedEvent>, IConsumer<CategoryUpdatedEvent>, IConsumer<CategoryDeletedEvent>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryConsumer> _logger;

        public CategoryConsumer(ICategoryRepository categoryRepository, ILogger<CategoryConsumer> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CategoryCreatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming CategoryCreatedEvent: {data.Name}");

            var existing = await _categoryRepository.GetByIdAsync(data.Id);
            if (existing == null)
            {
                var category = new Category
                {
                    Id = data.Id,
                    Name = data.Name,
                    Description = data.Description,
                    IsActive = data.IsActive,
                    StoreId = data.StoreId
                };

                await _categoryRepository.AddAsync(category);
                await _categoryRepository.SaveChangesAsync();
                _logger.LogInformation($"Category {data.Name} synced successfully (Created) in CatalogService.");
            }
            else
            {
                // UPSERT: Update if it already exists (useful for resync)
                existing.Name = data.Name;
                existing.Description = data.Description;
                existing.IsActive = data.IsActive;
                existing.StoreId = data.StoreId;
                _categoryRepository.Update(existing);

                await _categoryRepository.SaveChangesAsync();
                _logger.LogInformation($"Category {data.Name} synced successfully (Updated) in CatalogService.");
            }
        }

        public async Task Consume(ConsumeContext<CategoryUpdatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming CategoryUpdatedEvent: {data.Id} -> {data.Name}");

            var category = await _categoryRepository.GetByIdAsync(data.Id);
            if (category != null)
            {
                category.Name = data.Name;
                category.Description = data.Description;
                category.IsActive = data.IsActive;
                category.StoreId = data.StoreId;
                _categoryRepository.Update(category);

                await _categoryRepository.SaveChangesAsync();
                _logger.LogInformation($"Category {data.Id} updated successfully in CatalogService (Active: {data.IsActive}).");
            }
        }

        public async Task Consume(ConsumeContext<CategoryDeletedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming CategoryDeletedEvent: {data.Id}");

            var category = await _categoryRepository.GetByIdAsync(data.Id);
            if (category != null)
            {
                _categoryRepository.Delete(category);
                await _categoryRepository.SaveChangesAsync();
                _logger.LogInformation($"Category {data.Id} deleted successfully in CatalogService.");
            }
        }
    }
}
