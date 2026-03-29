using Microsoft.AspNetCore.Mvc;
using CatalogService.Interfaces;
using CatalogService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Staff")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _productService.GetAllProductsAsync());
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            return Ok(await _productService.SearchProductsAsync(q ?? ""));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            var result = await _productService.CreateProductAsync(productDto);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, new { message = "Product created successfully", product = result });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            await _productService.UpdateProductAsync(id, productDto);
            return Ok(new { message = "Product updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpPost("deduct")]
        public async Task<IActionResult> DeductStock([FromBody] List<StockUpdateDto> deductions)
        {
            await _productService.DeductStockAsync(deductions);
            return Ok(new { message = "Stock deducted successfully" });
        }

        [HttpPost("adjust-stock")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request)
        {
            var update = new List<StockUpdateDto> { new StockUpdateDto { ProductId = request.ProductId, Quantity = request.QuantityChange } };
            // Note: If QuantityChange is negative, it will effectively be an addition in DeductStock? 
            // Better to use AddStock or handle sign.
            if (request.QuantityChange >= 0)
                await _productService.AddStockAsync(update);
            else
                await _productService.DeductStockAsync(new List<StockUpdateDto> { new StockUpdateDto { ProductId = request.ProductId, Quantity = -request.QuantityChange } });
            
            return Ok(new { message = "Stock adjusted successfully" });
        }

        public class StockAdjustmentRequest { public int ProductId { get; set; } public int QuantityChange { get; set; } }
    }
}
