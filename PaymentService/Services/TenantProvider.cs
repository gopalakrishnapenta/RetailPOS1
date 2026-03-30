using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PaymentService.Interfaces;

namespace PaymentService.Services
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int StoreId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return 0;

                if (context.User.IsInRole("Admin") && context.Request.Headers.TryGetValue("X-Store-Id", out var headerStoreId))
                {
                    if (int.TryParse(headerStoreId, out int sid)) return sid;
                }

                var claim = context.User.FindFirst("StoreId");
                return (claim != null && int.TryParse(claim.Value, out int storeId)) ? storeId : 0;
            }
        }

        public string Role => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        public string? Token
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                var authHeader = context?.Request.Headers["Authorization"].ToString();
                return (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) 
                    ? authHeader.Substring("Bearer ".Length).Trim() : null;
            }
        }
    }
}
