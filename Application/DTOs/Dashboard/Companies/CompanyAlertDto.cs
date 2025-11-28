namespace Application.DTOs.Dashboard.Companies;

/// <summary>
/// Alert types for companies needing attention
/// </summary>
public enum CompanyAlertType
{
    NoSubscription,
    SubscriptionExpiring,
    SubscriptionExpired,
    SubscriptionSuspended,
    InactiveForLong
}

/// <summary>
/// Company alert item
/// </summary>
public record CompanyAlertDto(
    Guid CompanyId,
    string CompanyName,
    CompanyAlertType AlertType,
    string AlertMessage,
    DateTime? ExpiryDate,
    int DaysRemaining,
    string Priority,            // "critical", "high", "medium", "low"
    string SuggestedAction
);
