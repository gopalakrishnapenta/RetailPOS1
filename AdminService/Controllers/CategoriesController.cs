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
        private readonly ILogger<CategoriesController> _logger;
 
        public CategoriesController(AdminDbContext context, IPublishEndpoint publishEndpoint, ILogger<CategoriesController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
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

            _logger.LogInformation($"[SYNC] Publishing CategoryCreatedEvent for {category.Name} (Id: {category.Id})");
            // Publish Event
            await _publishEndpoint.Publish<CategoryCreatedEvent>(new
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                StoreId = category.StoreId
            });



            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, AdminCategoryEntity category)
        {
            if (category.Id == 0) category.Id = id;
            if (id != category.Id) return BadRequest(new { Message = "ID mismatch." });


            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Publish Event
            await _publishEndpoint.Publish<CategoryUpdatedEvent>(new
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                StoreId = category.StoreId
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
                Description = category.Description,
                IsActive = category.IsActive,
                StoreId = category.StoreId
            });


            return NoContent();
        }

        [HttpPost("sync")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncCategories()

        {
            var categories = await _context.Categories.ToListAsync();
            foreach (var category in categories)
            {
                await _publishEndpoint.Publish<CategoryCreatedEvent>(new
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    StoreId = category.StoreId
                });

            }
            return Ok(new { Message = $"Sync initiated for {categories.Count} categories." });
        }
    }
}
