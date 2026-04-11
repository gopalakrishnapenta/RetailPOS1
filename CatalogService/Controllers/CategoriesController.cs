using Microsoft.AspNetCore.Mvc;
using CatalogService.Interfaces;
using CatalogService.DTOs;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Catalog.CategoriesView)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _categoryService.GetAllCategoriesAsync());
        }

        [HttpGet("all")]
        [Authorize(Policy = Permissions.Catalog.CategoriesView)]
        public async Task<IActionResult> GetAllUnfiltered()
        {
            return Ok(await _categoryService.GetAllUnfilteredCategoriesAsync());
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.Catalog.CategoriesView)]
        public async Task<IActionResult> Get(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(category);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            var result = await _categoryService.CreateCategoryAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
        {
            await _categoryService.UpdateCategoryAsync(id, dto);
            return Ok(new { message = "Category updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(new { message = "Category deleted successfully" });
        }

        [HttpPost("{id}/restore")]
        [Authorize(Policy = Permissions.Catalog.CategoriesEdit)]
        public async Task<IActionResult> Restore(int id)
        {
            await _categoryService.RestoreCategoryAsync(id);
            return Ok(new { message = "Category restored successfully" });
        }
    }
}
