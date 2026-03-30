using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;
using MassTransit;
using RetailPOS.Contracts;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public CategoriesController(AdminDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Catalog.CategoriesView)]
        public async Task<ActionResult<IEnumerable<AdminCategoryEntity>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpGet("all")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.System.All)]
        public async Task<ActionResult<IEnumerable<AdminCategoryEntity>>> GetAllCategories()
        {
            // Ignore query filters to see soft-deleted (IsActive = false) items
            return await _context.Categories.IgnoreQueryFilters().ToListAsync();
        }

        [HttpPost]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Catalog.CategoriesEdit)]
        public async Task<ActionResult<AdminCategoryEntity>> CreateCategory(AdminCategoryEntity category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<CategoryCreatedEvent>(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.StoreId
            });

            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> UpdateCategory(int id, AdminCategoryEntity category)
        {
            if (id != category.Id) return BadRequest();
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.StoreId
            });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.StoreId
            });

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> RestoreCategory(int id)
        {
            // Use IgnoreQueryFilters to find the deactivated one
            var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            category.IsActive = true; 
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.StoreId
            });

            return NoContent();
        }
    }
}
