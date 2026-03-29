using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Interfaces;

namespace CatalogService.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(CatalogDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync()
        {
            return await _dbSet.Include(p => p.Category).ToListAsync();
        }

        public async Task<Product?> GetProductWithCategoryByIdAsync(int id)
        {
            return await _dbSet.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            return await _dbSet.Include(p => p.Category).FirstOrDefaultAsync(p => p.Sku == sku);
        }

        public async Task<Product?> GetByIdIgnoringFiltersAsync(int id)
        {
            return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        }
    }

    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(CatalogDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetByIdIgnoringFiltersAsync(int id)
        {
            return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
