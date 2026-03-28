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
    public class StoresController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public StoresController(AdminDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminStoreEntity>>> GetStores()
        {
            return await _context.Stores.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<AdminStoreEntity>> CreateStore(AdminStoreEntity store)
        {
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            // Publish Event via RabbitMQ
            await _publishEndpoint.Publish<StoreCreatedEvent>(new
            {
                Id = store.Id,
                StoreCode = store.StoreCode,
                Name = store.Name
            });

            return CreatedAtAction(nameof(GetStores), new { id = store.Id }, store);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(int id, AdminStoreEntity store)
        {
            if (id != store.Id) return BadRequest();

            _context.Entry(store).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Publish Update Event
            await _publishEndpoint.Publish<StoreUpdatedEvent>(new
            {
                Id = store.Id,
                StoreCode = store.StoreCode,
                Name = store.Name
            });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            // In a real system, you might set IsActive = false instead.
            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            // Publish Delete Event
            await _publishEndpoint.Publish<StoreDeletedEvent>(new
            {
                Id = store.Id,
                StoreCode = store.StoreCode
            });

            return NoContent();
        }
    }
}
