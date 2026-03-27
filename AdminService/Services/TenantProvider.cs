using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using AdminService.Interfaces;

namespace AdminService.Services
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

                // Admin Override from Header
                if (context.User.IsInRole("Admin") && context.Request.Headers.TryGetValue("X-Store-Id", out var headerStoreId))
                {
                    if (int.TryParse(headerStoreId, out int sid)) return sid;
                }

                var claim = context.User.FindFirst("StoreId");
                if (claim != null && int.TryParse(claim.Value, out int storeId))
                {
                    return storeId;
                }
                return 0;
            }
        }

        public string Role
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                return context?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            }
        }

        public string? Token
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                var authHeader = context?.Request.Headers["Authorization"].ToString();
                if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authHeader.Substring("Bearer ".Length).Trim();
                }
                return null;
            }
        }
    }
}
