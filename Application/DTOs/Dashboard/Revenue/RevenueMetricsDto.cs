namespace Application.DTOs.Dashboard.Revenue;

/// <summary>
/// Core revenue metrics
/// </summary>
public record RevenueMetricsDto(
    // Monthly Recurring Revenue
    decimal MRR,
    decimal PreviousMRR,
    decimal MRRChange,
    decimal MRRChangePercentage,
    
    // Annual Recurring Revenue
    decimal ARR,
    decimal PreviousARR,
    decimal ARRChange,
    decimal ARRChangePercentage,
    
    // Average Revenue Per Customer
    decimal ARPC,
    decimal PreviousARPC,
    decimal ARPCChange,
    decimal ARPCChangePercentage,
    
    // Customer Lifetime Value (estimated)
    decimal EstimatedCLTV,
    
    // Active paying customers
    int ActiveCustomers,
    int PreviousActiveCustomers
);
