namespace WebAPI.Configurations
{
    public static class CorsConfiguration
    {
        public static string _policyName = "AllowSpecificOrigin";

        public static IServiceCollection AddCorsConfigurationOptions(this IServiceCollection services)
        {
            // Define allowed origins
            var allowedOrigins = new[]
            {
                "http://localhost",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5000",
                "http://localhost:5001",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173",
                "https://synflox-frontend.vercel.app"
            };

            services.AddCors(options =>
            {
                options.AddPolicy(_policyName, builder =>
                {
                    var customOrigin = Environment.GetEnvironmentVariable("ALLOW_ORIGIN");
                    
                    if (!string.IsNullOrEmpty(customOrigin))
                    {
                        // If environment variable is set, use it and add to allowed origins
                        var origins = allowedOrigins.Concat(new[] { customOrigin }).Distinct().ToArray();
                        builder.WithOrigins(origins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    }
                    else
                    {
                        // Use predefined allowed origins
                        builder.WithOrigins(allowedOrigins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    }
                });
            });

            return services;
        }
    }
}
