using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Middleware;

/// <summary>
/// Middleware that extracts tenant ID from request and sets it in HttpContext.
/// </summary>
public class TenantContextMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Try to get tenant ID from header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader.ToString(), out var tenantId))
            {
                context.Items["TenantId"] = tenantId;
            }
        }
        // Try to get from JWT claim (if authenticated)
        else if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var claimTenantId))
            {
                context.Items["TenantId"] = claimTenantId;
            }
        }

        await next(context);
    }
}



