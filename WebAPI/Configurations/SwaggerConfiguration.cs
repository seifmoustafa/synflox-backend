using Microsoft.OpenApi.Models;

namespace WebAPI.Configurations
{
    public static class SwaggerConfiguration
    {
        public const string GroupName = "Administration";
        public static IServiceCollection AddSwaggerGenConfigurationOptions(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = ".Net Login Project Clean Architecture Api",
                    Version = "v1",
                    Description = "API for .Net Login Project Clean Architecture App"
                });

                options.EnableAnnotations();


                //options.TagActionsBy(api => [api.GroupName]);
                //options.DocInclusionPredicate((docName, api) => true);

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {{
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id="Bearer"
                    }
                } ,  Array.Empty<string>()

             }});

            });

            return services;
        }
    }
}
