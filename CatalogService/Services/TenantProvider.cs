using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using CatalogService.Interfaces;

namespace CatalogService.Services
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
                if (context?.User == null) return 0;

                // Check Role robustly to determine if we should allow header override
                string role = this.Role;

                // Admin Override from Header
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && 
                    context.Request.Headers.TryGetValue("X-Store-Id", out var headerStoreId))
                {
                    if (int.TryParse(headerStoreId, out int sid)) return sid;
                }

                // Try to get StoreId from various claim names
                var claim = context.User.FindFirst("StoreId") ?? 
                            context.User.FindFirst("storeid") ??
                            context.User.FindFirst("Storeid") ??
                            context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid") ??
                            context.User.FindFirst(ClaimTypes.Sid);

                if (claim != null && int.TryParse(claim.Value, out int storeId))
                {
                    return storeId;
                }

                return 0; // Global or unauthenticated
            }
        }

        public string Role
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.User == null) return string.Empty;

                var roleClaim = context.User.FindFirst(ClaimTypes.Role) ?? 
                                context.User.FindFirst("role") ?? 
                                context.User.FindFirst("Role") ??
                                context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                
                return roleClaim?.Value ?? string.Empty;
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
