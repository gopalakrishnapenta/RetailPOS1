using CatalogService.Models;
using System.Linq.Expressions;

namespace CatalogService.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsWithCategoryAsync();
        Task<Product?> GetProductWithCategoryByIdAsync(int id);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task<Product?> GetByIdIgnoringFiltersAsync(int id);
    }

    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetByIdIgnoringFiltersAsync(int id);
    }
}
