using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Domain.Entities.Common;

namespace WebAPI.Configurations;

/// <summary>
/// Helper class to generate example responses for Swagger documentation
/// </summary>
public static class SwaggerExamples
{
    public static ApiResponse<CompanyDto> CompanyResponseExample => new(
        200,
        "Company retrieved successfully",
        new CompanyDto
        {
            Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Name = "Acme Corporation",
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            ContactEmail = "admin@acme.com",
            ContactPhone = "+1234567890",
            Address = "123 Main St, City, Country",
            LicenseKey = "encrypted_license_key_here",
            IsTrial = false,
            TrialEndDate = null,
            SubscriptionPlanId = Guid.Parse("660e8400-e29b-41d4-a716-446655440001"),
            CreatedTimestamp = DateTime.UtcNow.AddMonths(-6),
            UpdatedTimestamp = DateTime.UtcNow
        }
    );

    public static ApiResponse<object> CompaniesListResponseExample => new(
        200,
        "Companies retrieved successfully",
        new
        {
            companies = new[]
            {
                new CompanyDto
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                    Name = "Acme Corporation",
                    IsActive = true,
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    ContactEmail = "admin@acme.com",
                    ContactPhone = "+1234567890",
                    Address = "123 Main St, City, Country",
                    IsTrial = false,
                    CreatedTimestamp = DateTime.UtcNow.AddMonths(-6),
                    UpdatedTimestamp = DateTime.UtcNow
                }
            },
            pagination = new PaginationMetadata(100, 10, 1)
        }
    );

    public static ApiResponse<string> ErrorResponseExample => new(
        400,
        "An error occurred",
        null,
        new[] { "Error message 1", "Error message 2" }
    );

    public static ApiResponse<string> NotFoundResponseExample => new(
        404,
        "Resource not found"
    );

    public static ApiResponse<string> SuccessResponseExample => new(
        200,
        "Operation completed successfully"
    );
}





