using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Configurations
{
    public static class AuthorizationConfiguration
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdminOnly", policy =>
                    policy.RequireClaim(JwtClaimTypes.AdminTypeName, "SuperAdmin"));
                options.AddPolicy("AdminOrSuperAdmin", policy =>
                    policy.RequireClaim(JwtClaimTypes.AdminTypeName, "Admin", "SuperAdmin"));
            });

            return services;
        }
    }
}
