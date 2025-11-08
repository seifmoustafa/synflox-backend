using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace WebAPI.Configurations;

/// <summary>
/// Schema filter to add examples to Swagger documentation
/// </summary>
public class SwaggerExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(ApiResponse<CompanyDto>))
        {
            schema.Example = new OpenApiObject
            {
                ["statusCode"] = new OpenApiInteger(200),
                ["message"] = new OpenApiString("Company retrieved successfully"),
                ["data"] = new OpenApiObject
                {
                    ["id"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                    ["name"] = new OpenApiString("Acme Corporation"),
                    ["isActive"] = new OpenApiBoolean(true),
                    ["expiryDate"] = new OpenApiString(DateTime.UtcNow.AddYears(1).ToString("O")),
                    ["contactEmail"] = new OpenApiString("admin@acme.com"),
                    ["contactPhone"] = new OpenApiString("+1234567890"),
                    ["address"] = new OpenApiString("123 Main St, City, Country"),
                    ["isTrial"] = new OpenApiBoolean(false),
                    ["createdTimestamp"] = new OpenApiString(DateTime.UtcNow.AddMonths(-6).ToString("O")),
                    ["updatedTimestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O"))
                }
            };
        }
        else if (context.Type == typeof(ApiResponse<string>))
        {
            schema.Example = new OpenApiObject
            {
                ["statusCode"] = new OpenApiInteger(400),
                ["message"] = new OpenApiString("An error occurred"),
                ["data"] = new OpenApiNull(),
                ["errors"] = new OpenApiArray
                {
                    new OpenApiString("Error message 1"),
                    new OpenApiString("Error message 2")
                }
            };
        }
    }
}





