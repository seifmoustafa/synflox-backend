namespace WebAPI.Configurations
{
    public static class CorsConfiguration
    {
        public static string _policyName = "AllowSpecificOrigin";

        public static IServiceCollection AddCorsConfigurationOptions(this IServiceCollection services)
        {

            services.AddCors(options =>
            {
                options.AddPolicy(_policyName, builder =>
                {
                    var origin = Environment.GetEnvironmentVariable("ALLOW_ORIGIN");
                    if (!string.IsNullOrEmpty(origin))
                    {
                        builder.WithOrigins(origin)
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    }
                    else
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    }
                });
            });


            return services;
        }
    }
}
