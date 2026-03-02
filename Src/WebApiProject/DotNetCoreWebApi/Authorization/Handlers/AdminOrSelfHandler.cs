using DotNetCoreWebApi.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DotNetCoreWebApi.Authorization.Handlers;

public class AdminOrSelfHandler : AuthorizationHandler<AdminOrSelfRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrSelfRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!TryGetRouteUserId(context.Resource, out var routeUserId))
        {
            return Task.CompletedTask;
        }

        var subject = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(subject, out var subjectUserId) && subjectUserId == routeUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool TryGetRouteUserId(object? resource, out int routeUserId)
    {
        routeUserId = 0;

        string? id = resource switch
        {
            HttpContext httpContext => httpContext.Request.RouteValues["id"]?.ToString(),
            AuthorizationFilterContext filterContext => filterContext.RouteData.Values["id"]?.ToString(),
            _ => null
        };

        return int.TryParse(id, out routeUserId);
    }
}
