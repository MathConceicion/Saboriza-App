namespace GastronomiaMarketplace.WebAPI.Policies;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public sealed class VendorOwnerHandler : AuthorizationHandler<VendorOwnerRequirement, IOwnedByVendor>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VendorOwnerRequirement requirement,
        IOwnedByVendor resource)
    {
        if (context.User.IsInRole("Admin") || context.User.FindFirstValue("is_admin") == "true")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        var vendorIdClaim = context.User.FindFirstValue("vendor_id");
        if (Guid.TryParse(vendorIdClaim, out var vendorId) && vendorId == resource.VendorId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}

public sealed class AdminBypassHandler : AuthorizationHandler<VendorOwnerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, VendorOwnerRequirement requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.FindFirstValue("is_admin") == "true")
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
