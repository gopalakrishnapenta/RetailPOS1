using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RetailPOS.Common.Authorization
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRetailPOSAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            
            return services;
        }
    }

    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if the policy already exists (e.g. "AdminOnly" if we keep it)
            var policy = await base.GetPolicyAsync(policyName);
            
            if (policy == null)
            {
                // Dynamic policy creation for any [Authorize(Policy="resource:action")]
                policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
            }

            return policy;
        }
    }
}
