using Microsoft.AspNetCore.Mvc;
using CatalogService.Interfaces;
using RetailPOS.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly CatalogService.Data.CatalogDbContext _context;

        public SyncController(IProductService productService, IPublishEndpoint publishEndpoint, CatalogService.Data.CatalogDbContext context)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
            _context = context;
        }

        [HttpPost("products")]
        public async Task<IActionResult> SyncAllProducts()
        {
            var products = await _context.Products.IgnoreQueryFilters().ToListAsync();
            int count = 0;

            foreach (var p in products)
            {
                await _publishEndpoint.Publish<ProductCreatedEvent>(new
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    InitialStock = p.StockQuantity,
                    StoreId = p.StoreId
                });
                count++;
            }

            return Ok(new { Message = $"Triggered sync for {count} products.", Count = count });
        }
    }
}
