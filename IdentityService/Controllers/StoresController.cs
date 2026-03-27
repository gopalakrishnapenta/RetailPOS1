using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StoresController : ControllerBase
    {
        private readonly IStoreRepository _storeRepository;

        public StoresController(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveStores()
        {
            var stores = await _storeRepository.FindAsync(s => s.IsActive);
            return Ok(stores.Select(s => new { s.Id, s.StoreCode, s.Name }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStore(int id)
        {
            var store = await _storeRepository.GetByIdAsync(id);
            if (store == null) return NotFound();
            return Ok(store);
        }
    }
}
