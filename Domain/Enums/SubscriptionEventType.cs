namespace Domain.Enums;

/// <summary>
/// Types of subscription lifecycle events for domain events and notifications
/// </summary>
public enum SubscriptionEventType
{
    Created = 1,
    Activated = 2,
    Expired = 3,
    Suspended = 4,
    Resumed = 5,
    Renewed = 6,
    Upgraded = 7,
    Canceled = 8,
    TrialStarted = 9,
    TrialExpired = 10,
    TrialConverted = 11,
    DeferredActivated = 12,
    AutoRenewed = 13
}
