namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription lifecycle metrics for a period
/// </summary>
public record LifecycleMetricsDto(
    // This Period (default: this month)
    int NewSubscriptions,
    int Renewals,
    int Upgrades,
    int Downgrades,
    int Cancellations,
    int Suspensions,
    int Reactivations,
    
    // Comparison with previous period
    int PreviousNewSubscriptions,
    int PreviousRenewals,
    int PreviousCancellations,
    
    // Rates
    decimal TrialConversionRate,    // Trials converted to paid %
    decimal ChurnRate,              // Cancelled / Total active %
    decimal RetentionRate,          // 100 - ChurnRate
    decimal RenewalRate,            // Renewed / Expiring %
    
    // Average Duration
    int AverageDurationDays,
    int MedianDurationDays
);
