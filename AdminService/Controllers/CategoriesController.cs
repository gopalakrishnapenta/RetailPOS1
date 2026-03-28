using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MassTransit;
using RetailPOS.Contracts;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
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
        public async Task<ActionResult<IEnumerable<AdminCategoryEntity>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AdminCategoryEntity>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return category;
        }

        [HttpPost]
        public async Task<ActionResult<AdminCategoryEntity>> CreateCategory(AdminCategoryEntity category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Publish Event
            await _publishEndpoint.Publish<CategoryCreatedEvent>(new
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            });

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, AdminCategoryEntity category)
        {
            if (id != category.Id) return BadRequest();

            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Publish Event
            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = false;
            await _context.SaveChangesAsync();

            // Publish Event (Sync as Update since it's just a status change)
            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            });

            return NoContent();
        }
    }
}
