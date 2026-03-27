using Microsoft.AspNetCore.Mvc;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class StoresController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly HttpClient _httpClient;

        public StoresController(AdminDbContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpClient = httpFactory.CreateClient();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Store>>> GetStores()
        {
            return await _context.Stores.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Store>> CreateStore(Store store)
        {
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            // Sync with IdentityService
            try {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeader.Replace("Bearer ", ""));
                }
                await _httpClient.PostAsJsonAsync("http://localhost:5001/api/auth/stores", new { store.StoreCode, store.Name });
            } catch (Exception ex) {
                Console.WriteLine($"Store sync failed: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetStores), new { id = store.Id }, store);
        }
    }
}
