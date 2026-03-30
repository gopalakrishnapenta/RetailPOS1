using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RetailPOS.Common.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // If the user has the "all:all" permission (Super Admin), always allow
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == "all:all"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Otherwise, check for the specific granular permission
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
