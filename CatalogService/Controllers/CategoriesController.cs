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

        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.Catalog.CategoriesView)]
        public async Task<IActionResult> Get(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(category);
        }
    }
}
