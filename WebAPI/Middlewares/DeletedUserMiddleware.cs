using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebAPI.Middlewares;

/// <summary>
/// Middleware to check if authenticated user has been deleted (soft delete)
/// and reject their requests even if JWT is still valid
/// </summary>
public class DeletedUserMiddleware
{
    private readonly RequestDelegate _next;

    public DeletedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAdminRepository adminRepository, ICurrentUserService currentUserService)
    {
        // Only check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var userId = currentUserService.UserId;
                var admin = await adminRepository.GetByIdAsync(userId, null);

                // If admin is deleted or deactivated, reject the request
                if (admin == null || admin.IsDeleted || !admin.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Account has been deactivated or deleted\"}");
                    return;
                }
            }
            catch
            {
                // If CurrentUserService throws (no user), continue normally
                // This can happen during migrations, background jobs, etc.
            }
        }

        await _next(context);
    }
}
