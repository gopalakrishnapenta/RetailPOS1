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
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.StoresView)]
        public async Task<ActionResult<IEnumerable<AdminStoreEntity>>> GetStores()
        {
            return await _context.Stores.ToListAsync();
        }

        [HttpGet("all")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.System.All)]
        public async Task<ActionResult<IEnumerable<AdminStoreEntity>>> GetAllStores()
        {
            // Admin only: See all stores including soft-deleted (IsActive = false)
            return await _context.Stores.IgnoreQueryFilters().ToListAsync();
        }

        [HttpPost]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.StoresManage)]
        public async Task<ActionResult<AdminStoreEntity>> CreateStore(AdminStoreEntity store)
        {
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<StoreCreatedEvent>(new
            {
                store.Id,
                store.StoreCode,
                store.Name,
                store.Location,
                store.IsActive
            });

            return CreatedAtAction(nameof(GetStores), new { id = store.Id }, store);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.StoresManage)]
        public async Task<IActionResult> UpdateStore(int id, AdminStoreEntity store)
        {
            // Set ID from path if missing in body
            if (store.Id == 0) store.Id = id;

            if (id != store.Id) 
                return BadRequest(new { message = "Path ID and Body ID mismatch." });

            _context.Entry(store).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<StoreUpdatedEvent>(new
            {
                store.Id,
                store.StoreCode,
                store.Name,
                store.Location,
                store.IsActive
            });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.StoresManage)]
        public async Task<IActionResult> DeleteStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            store.IsActive = false; // Soft-delete
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<StoreUpdatedEvent>(new
            {
                store.Id,
                store.StoreCode,
                store.Name,
                store.Location,
                store.IsActive
            });

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.StoresManage)]
        public async Task<IActionResult> RestoreStore(int id)
        {
            // Admin only: Restore a previously deactivated store
            var store = await _context.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (store == null) return NotFound();

            store.IsActive = true;
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish<StoreUpdatedEvent>(new
            {
                store.Id,
                store.StoreCode,
                store.Name,
                store.Location,
                store.IsActive
            });

            return NoContent();
        }
    }
}
