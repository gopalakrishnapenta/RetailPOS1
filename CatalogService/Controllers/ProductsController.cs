using Microsoft.AspNetCore.Mvc;
using CatalogService.Interfaces;
using CatalogService.DTOs;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Catalog.View)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _productService.GetAllProductsAsync());
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Catalog.Manage)]
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            var result = await _productService.CreateProductAsync(productDto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Catalog.Manage)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            await _productService.UpdateProductAsync(id, productDto);
            return Ok(new { message = "Product updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Catalog.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully" });
        }
    }
}
