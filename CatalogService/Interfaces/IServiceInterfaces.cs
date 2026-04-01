using CatalogService.DTOs;

namespace CatalogService.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> GetAllUnfilteredProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductBySkuAsync(string sku);
        Task<IEnumerable<ProductDto>> SearchProductsAsync(string query);
        Task<ProductDto> CreateProductAsync(ProductDto productDto);
        Task<bool> UpdateProductAsync(int id, ProductDto productDto);
        Task<bool> DeductStockAsync(IEnumerable<StockUpdateDto> deductions);
        Task<bool> AddStockAsync(IEnumerable<StockUpdateDto> additions);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> RestoreProductAsync(int id);
    }

    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetAllUnfilteredCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto);
        Task<bool> UpdateCategoryAsync(int id, CategoryDto categoryDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> RestoreCategoryAsync(int id);
    }
}
