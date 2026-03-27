using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.DTOs;
using CatalogService.Interfaces;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetProductsWithCategoryAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var p = await _productRepository.GetProductWithCategoryByIdAsync(id);
            return p == null ? null : MapToDto(p);
        }

        public async Task<ProductDto?> GetProductBySkuAsync(string sku)
        {
            var p = await _productRepository.GetProductBySkuAsync(sku);
            return p == null ? null : MapToDto(p);
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string query)
        {
            var products = await _productRepository.GetProductsWithCategoryAsync();
            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => p.Name.Contains(query) || p.Barcode == query || p.Sku == query).ToList();
            }
            return products.Select(MapToDto);
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto dto)
        {
            var p = new Product
            {
                Sku = dto.Sku, Barcode = dto.Barcode, Name = dto.Name,
                MRP = dto.MRP, SellingPrice = dto.SellingPrice, TaxCode = dto.TaxCode,
                ReorderLevel = dto.ReorderLevel, IsActive = dto.IsActive,
                StockQuantity = dto.StockQuantity, CategoryId = dto.CategoryId
            };
            await _productRepository.AddAsync(p);
            await _productRepository.SaveChangesAsync();
            return await GetProductByIdAsync(p.Id) ?? MapToDto(p);
        }

        public async Task<bool> UpdateProductAsync(int id, ProductDto dto)
        {
            var p = await _productRepository.GetByIdAsync(id);
            if (p == null) return false;

            p.Name = dto.Name; p.MRP = dto.MRP; p.SellingPrice = dto.SellingPrice;
            p.TaxCode = dto.TaxCode; p.ReorderLevel = dto.ReorderLevel;
            p.IsActive = dto.IsActive; p.StockQuantity = dto.StockQuantity;
            p.CategoryId = dto.CategoryId; p.Sku = dto.Sku; p.Barcode = dto.Barcode;

            _productRepository.Update(p);
            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeductStockAsync(IEnumerable<StockUpdateDto> deductions)
        {
            foreach (var item in deductions)
            {
                var p = await _productRepository.GetByIdAsync(item.ProductId);
                if (p != null) p.StockQuantity -= item.Quantity;
            }
            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddStockAsync(IEnumerable<StockUpdateDto> additions)
        {
            foreach (var item in additions)
            {
                var p = await _productRepository.GetByIdAsync(item.ProductId);
                if (p != null) p.StockQuantity += item.Quantity;
            }
            await _productRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var p = await _productRepository.GetByIdAsync(id);
            if (p == null) return false;
            _productRepository.Delete(p);
            await _productRepository.SaveChangesAsync();
            return true;
        }

        private ProductDto MapToDto(Product p) => new ProductDto
        {
            Id = p.Id, Sku = p.Sku, Barcode = p.Barcode, Name = p.Name,
            MRP = p.MRP, SellingPrice = p.SellingPrice, TaxCode = p.TaxCode,
            ReorderLevel = p.ReorderLevel, StockQuantity = p.StockQuantity,
            CategoryId = p.CategoryId, CategoryName = p.Category?.Name ?? "Other",
            IsActive = p.IsActive, StoreId = p.StoreId
        };
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository) { _categoryRepository = categoryRepository; }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(c => new CategoryDto
            {
                Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive
            });
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            return c == null ? null : new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive };
        }

        public async Task<CategoryDto> CreateCategoryAsync(CategoryDto dto)
        {
            var c = new Category { Name = dto.Name, Description = dto.Description, IsActive = dto.IsActive };
            await _categoryRepository.AddAsync(c);
            await _categoryRepository.SaveChangesAsync();
            dto.Id = c.Id;
            return dto;
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            if (c == null) return false;
            c.Name = dto.Name; c.Description = dto.Description; c.IsActive = dto.IsActive;
            _categoryRepository.Update(c);
            await _categoryRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            if (c == null) return false;
            _categoryRepository.Delete(c);
            await _categoryRepository.SaveChangesAsync();
            return true;
        }
    }
}
