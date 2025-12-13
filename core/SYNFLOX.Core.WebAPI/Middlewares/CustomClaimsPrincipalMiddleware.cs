
using Infrastructure.Authentication;

namespace WebAPI.Middlewares
{
    /// <summary>
    /// Replaces the current <see cref="ClaimsPrincipal"/> with <see cref="CustomClaimsPrincipal"/>
    /// so user data from JWT claims can be easily accessed.
    /// </summary>
    public class CustomClaimsPrincipalMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                context.User = new CustomClaimsPrincipal(context.User);
            }

            // Call the next middleware in the pipeline
            await next(context);
        }
    }
}
