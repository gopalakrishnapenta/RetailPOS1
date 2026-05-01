using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.DTOs;
using CatalogService.Interfaces;
using CatalogService.Exceptions;
using RetailPOS.Contracts;
using MassTransit;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using RetailPOS.Common.Logging;
using Serilog;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ITenantProvider _tenantProvider;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProductService> _logger;

        private const string ALL_PRODUCTS_CACHE_KEY = "all_products_v1";

        public ProductService(
            IProductRepository productRepository, 
            ITenantProvider tenantProvider, 
            IPublishEndpoint publishEndpoint,
            IDistributedCache cache,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
            _publishEndpoint = publishEndpoint;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            // Try get from cache
            var cachedData = await _cache.GetStringAsync(ALL_PRODUCTS_CACHE_KEY);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("🚀 [CACHE] Hit for all products list.");
                return JsonSerializer.Deserialize<IEnumerable<ProductDto>>(cachedData) ?? Enumerable.Empty<ProductDto>();
            }

            _logger.LogInformation("🐢 [DATABASE] Cache miss for products. Fetching from DB...");
            var db = ((CatalogService.Repositories.ProductRepository)_productRepository).GetContext();
            var products = await db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var result = products.Where(p => p.Category == null || p.Category.IsActive).Select(MapToDto).ToList();

            // Store in cache for 10 minutes
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
            await _cache.SetStringAsync(ALL_PRODUCTS_CACHE_KEY, JsonSerializer.Serialize(result), cacheOptions);

            return result;
        }

        public async Task<IEnumerable<ProductDto>> GetAllUnfilteredProductsAsync()
        {
            var db = ((CatalogService.Repositories.ProductRepository)_productRepository).GetContext();
            var query = db.Products.IgnoreQueryFilters().Include(p => p.Category).AsQueryable();

            if (!IsGlobalAdmin())
            {
                query = query.Where(p => p.StoreId == _tenantProvider.StoreId);
            }

            var products = await query.ToListAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var p = await _productRepository.GetProductWithCategoryByIdAsync(id);
            if (p == null) throw new NotFoundException($"Product with ID {id} not found.");
            return MapToDto(p);
        }

        public async Task<ProductDto?> GetProductBySkuAsync(string sku)
        {
            var p = await _productRepository.GetProductBySkuAsync(sku);
            return p == null ? null : MapToDto(p);
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string query)
        {
            var products = await _productRepository.GetProductsWithCategoryAsync();
            products = products.Where(p => p.IsActive && (p.Category == null || p.Category.IsActive)).ToList();

            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => p.Name.Contains(query) || p.Barcode == query || p.Sku == query).ToList();
            }

            return products.Select(MapToDto);
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto dto)
        {
            var existing = await _productRepository.GetProductBySkuAsync(dto.Sku);
            if (existing != null) throw new ConflictException($"Product with SKU {dto.Sku} already exists.");

            var p = new Product
            {
                Sku = dto.Sku,
                Barcode = dto.Barcode,
                Name = dto.Name,
                MRP = dto.MRP,
                SellingPrice = dto.SellingPrice,
                TaxCode = dto.TaxCode,
                ReorderLevel = dto.ReorderLevel,
                IsActive = dto.IsActive,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId
            };

            await _productRepository.AddAsync(p);
            await _productRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_PRODUCTS_CACHE_KEY);

            // SYNC: Notify other services about the new product and its initial stock
            await _publishEndpoint.Publish<ProductCreatedEvent>(new
            {
                ProductId = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                InitialStock = p.StockQuantity,
                StoreId = p.StoreId
            });

            return MapToDto(p);
        }

        public async Task<bool> UpdateProductAsync(int id, ProductDto dto)
        {
            var p = await GetScopedProductQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) throw new NotFoundException($"Product with ID {id} not found.");

            p.Name = dto.Name;
            p.MRP = dto.MRP;
            p.SellingPrice = dto.SellingPrice;
            p.TaxCode = dto.TaxCode;
            p.ReorderLevel = dto.ReorderLevel;
            p.IsActive = dto.IsActive;
            p.StockQuantity = dto.StockQuantity;
            p.CategoryId = dto.CategoryId;
            p.Sku = dto.Sku;
            p.Barcode = dto.Barcode;

            _productRepository.Update(p);
            await _productRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_PRODUCTS_CACHE_KEY);

            // SYNC: Notify about updates
            await _publishEndpoint.Publish<ProductUpdatedEvent>(new
            {
                ProductId = p.Id,
                Name = p.Name,
                Sku = p.Sku
            });

            return true;
        }

        public async Task<bool> DeductStockAsync(IEnumerable<StockUpdateDto> deductions)
        {
            foreach (var item in deductions)
            {
                var p = await _productRepository.GetByIdIgnoringFiltersAsync(item.ProductId);
                if (p != null)
                {
                    p.StockQuantity -= item.Quantity;
                    
                    // SYNC: Notify dashboard and other services
                    await _publishEndpoint.Publish<StockAdjustedEvent>(new
                    {
                        ProductId = p.Id,
                        StoreId = p.StoreId,
                        QuantityChange = -item.Quantity,
                        ReasonCode = "Adjustment-Deduction",
                        DocumentReference = "Manual Adjustment"
                    });
                }
            }

            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddStockAsync(IEnumerable<StockUpdateDto> additions)
        {
            foreach (var item in additions)
            {
                var p = await _productRepository.GetByIdIgnoringFiltersAsync(item.ProductId);
                if (p != null)
                {
                    p.StockQuantity += item.Quantity;

                    // SYNC: Notify dashboard and other services
                    await _publishEndpoint.Publish<StockAdjustedEvent>(new
                    {
                        ProductId = p.Id,
                        StoreId = p.StoreId,
                        QuantityChange = item.Quantity,
                        ReasonCode = "Adjustment-Addition",
                        DocumentReference = "Manual Adjustment"
                    });
                }
            }

            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var p = await GetScopedProductQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) throw new NotFoundException($"Product with ID {id} not found.");

            p.IsActive = false;
            _productRepository.Update(p);
            await _productRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_PRODUCTS_CACHE_KEY);
            return true;
        }

        public async Task<bool> RestoreProductAsync(int id)
        {
            var p = await GetScopedProductQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) throw new NotFoundException($"Product with ID {id} not found.");

            p.IsActive = true;
            _productRepository.Update(p);
            await _productRepository.SaveChangesAsync();
            return true;
        }

        private IQueryable<Product> GetScopedProductQuery()
        {
            var db = ((CatalogService.Repositories.ProductRepository)_productRepository).GetContext();
            var query = db.Products.IgnoreQueryFilters().AsQueryable();

            if (!IsGlobalAdmin())
            {
                query = query.Where(x => x.StoreId == _tenantProvider.StoreId);
            }

            return query;
        }

        private ProductDto MapToDto(Product p) => new ProductDto
        {
            Id = p.Id,
            Sku = p.Sku,
            Barcode = p.Barcode,
            Name = p.Name,
            MRP = p.MRP,
            SellingPrice = p.SellingPrice,
            TaxCode = p.TaxCode,
            ReorderLevel = p.ReorderLevel,
            StockQuantity = p.StockQuantity,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "Other",
            IsActive = p.IsActive,
            StoreId = p.StoreId
        };

        private bool IsGlobalAdmin() => _tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0;
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CategoryService> _logger;

        private const string ALL_CATEGORIES_CACHE_KEY = "all_categories_v1";

        public CategoryService(ICategoryRepository categoryRepository, IDistributedCache cache, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var cachedData = await _cache.GetStringAsync(ALL_CATEGORIES_CACHE_KEY);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("🚀 [CACHE] Hit for all categories list.");
                return JsonSerializer.Deserialize<IEnumerable<CategoryDto>>(cachedData) ?? Enumerable.Empty<CategoryDto>();
            }

            _logger.LogInformation("🐢 [DATABASE] Cache miss for categories. Fetching from DB...");
            var db = ((CatalogService.Repositories.CategoryRepository)_categoryRepository).GetContext();
            var categories = await db.Categories.IgnoreQueryFilters()
                .Where(c => c.IsActive)
                .ToListAsync();

            var result = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToList();

            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            await _cache.SetStringAsync(ALL_CATEGORIES_CACHE_KEY, JsonSerializer.Serialize(result), cacheOptions);

            return result;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllUnfilteredCategoriesAsync()
        {
            var db = ((CatalogService.Repositories.CategoryRepository)_categoryRepository).GetContext();
            var categories = await db.Categories.IgnoreQueryFilters().ToListAsync();
            return categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            });
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            if (c == null) throw new NotFoundException($"Category with ID {id} not found.");
            return new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive };
        }

        public async Task<CategoryDto> CreateCategoryAsync(CategoryDto dto)
        {
            var c = new Category { Name = dto.Name, Description = dto.Description, IsActive = dto.IsActive };
            await _categoryRepository.AddAsync(c);
            await _categoryRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_CATEGORIES_CACHE_KEY);
            dto.Id = c.Id;
            return dto;
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            var db = ((CatalogService.Repositories.CategoryRepository)_categoryRepository).GetContext();
            var c = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) throw new NotFoundException($"Category with ID {id} not found.");
            c.Name = dto.Name;
            c.Description = dto.Description;
            c.IsActive = dto.IsActive;
            await _categoryRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_CATEGORIES_CACHE_KEY);
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var db = ((CatalogService.Repositories.CategoryRepository)_categoryRepository).GetContext();
            var c = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) throw new NotFoundException($"Category with ID {id} not found.");

            c.IsActive = false;
            await _categoryRepository.SaveChangesAsync();

            // Invalidate Cache
            await _cache.RemoveAsync(ALL_CATEGORIES_CACHE_KEY);
            return true;
        }

        public async Task<bool> RestoreCategoryAsync(int id)
        {
            var c = await _categoryRepository.GetByIdIgnoringFiltersAsync(id);
            if (c == null) throw new NotFoundException($"Category with ID {id} not found.");

            c.IsActive = true;
            _categoryRepository.Update(c);
            await _categoryRepository.SaveChangesAsync();
            return true;
        }
    }
}
