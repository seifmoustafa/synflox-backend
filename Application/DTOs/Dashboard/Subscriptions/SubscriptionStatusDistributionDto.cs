namespace Application.DTOs.Dashboard.Subscriptions;

/// <summary>
/// Subscription status distribution
/// </summary>
public record SubscriptionStatusDistributionDto(
    int Active,
    int Trial,
    int Expired,
    int Suspended,
    int Cancelled,
    int Paused,
    int Total
);
