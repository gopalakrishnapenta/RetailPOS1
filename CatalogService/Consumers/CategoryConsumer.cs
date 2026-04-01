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
            _logger.LogInformation($"Consuming CategoryCreatedEvent: {data.Id} -> {data.Name}");

            await UpsertCategory(data.Id, data.Name, data.Description, data.IsActive, data.StoreId);
        }

        public async Task Consume(ConsumeContext<CategoryUpdatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming CategoryUpdatedEvent: {data.Id} -> {data.Name}");

            await UpsertCategory(data.Id, data.Name, data.Description, data.IsActive, data.StoreId);
        }

        private async Task UpsertCategory(int id, string name, string description, bool isActive, int storeId)
        {
            var category = await _categoryRepository.GetByIdIgnoringFiltersAsync(id);
            if (category == null)
            {
                category = new Category
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    IsActive = isActive,
                    StoreId = storeId
                };
                await _categoryRepository.AddAsync(category);
                _logger.LogInformation($"Category {id} created via synchronization.");
            }
            else
            {
                category.Name = name;
                category.Description = description;
                category.IsActive = isActive;
                category.StoreId = storeId;
                _categoryRepository.Update(category);
                _logger.LogInformation($"Category {id} updated via synchronization.");
            }

            await _categoryRepository.SaveChangesAsync();
        }

        public async Task Consume(ConsumeContext<CategoryDeletedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming CategoryDeletedEvent: {data.Id}");

            var category = await _categoryRepository.GetByIdIgnoringFiltersAsync(data.Id);
            if (category != null)
            {
                _categoryRepository.Delete(category);
                await _categoryRepository.SaveChangesAsync();
                _logger.LogInformation($"Category {data.Id} deleted successfully in CatalogService.");
            }
        }
    }
}
