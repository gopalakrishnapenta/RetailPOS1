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
    }
}
