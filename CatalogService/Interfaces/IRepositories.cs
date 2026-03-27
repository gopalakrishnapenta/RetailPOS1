using CatalogService.Models;
using System.Linq.Expressions;

namespace CatalogService.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsWithCategoryAsync();
        Task<Product?> GetProductWithCategoryByIdAsync(int id);
        Task<Product?> GetProductBySkuAsync(string sku);
    }

    public interface ICategoryRepository : IGenericRepository<Category>
    {
    }
}
