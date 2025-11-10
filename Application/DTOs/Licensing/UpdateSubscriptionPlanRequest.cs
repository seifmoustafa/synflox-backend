using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for updating a subscription plan.
/// </summary>
public class UpdateSubscriptionPlanRequest
{
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal? Price { get; set; }

    [StringLength(3, ErrorMessage = "Currency must be 3 characters")]
    public string? Currency { get; set; }

    public BillingCycle? BillingCycle { get; set; }

    public bool? IsActive { get; set; }

    public string[]? Features { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Max companies must be positive")]
    public int? MaxCompanies { get; set; }

    public PlanTier? PlanTier { get; set; }

    public string? ParentPlanId { get; set; }
}

