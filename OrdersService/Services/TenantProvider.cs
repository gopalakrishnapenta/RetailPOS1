using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrdersService.Interfaces;

namespace OrdersService.Services
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

                // Check Role robustly to determine if we should allow header override
                string role = this.Role;

                // Admin Override from Header
                if (role == "Admin" && context.Request.Headers.TryGetValue("X-Store-Id", out var headerStoreId))
                {
                    if (int.TryParse(headerStoreId, out int sid)) return sid;
                }

                var claim = context.User.FindFirst("StoreId") ?? 
                            context.User.FindFirst("storeid") ??
                            context.User.FindFirst("Storeid");

                if (claim != null && int.TryParse(claim.Value, out int storeId))
                {
                    return storeId;
                }
                return 0;
            }
        }

        public int UserId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return 0;
                var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ??
                            context.User.FindFirst("sub") ??
                            context.User.FindFirst("id");

                if (claim != null && int.TryParse(claim.Value, out int userId))
                {
                    return userId;
                }
                return 0;
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
