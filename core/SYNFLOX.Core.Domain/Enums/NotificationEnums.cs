namespace Domain.Enums;

/// <summary>
/// Types of notifications in the system
/// </summary>
public enum NotificationType
{
    // Subscription Events
    SubscriptionExpiring30Days = 1,
    SubscriptionExpiring7Days = 2,
    SubscriptionExpiring1Day = 3,
    SubscriptionExpired = 4,
    SubscriptionCreated = 5,
    SubscriptionRenewed = 6,
    SubscriptionUpgraded = 7,
    SubscriptionDowngraded = 8,
    SubscriptionCancelled = 9,
    SubscriptionSuspended = 10,
    SubscriptionReactivated = 11,
    
    // Trial Events
    TrialStarted = 20,
    TrialExpiring = 21,
    TrialExpired = 22,
    
    // Device Events
    DeviceLimitReached = 30,
    DeviceBound = 31,
    DeviceUnbound = 32,
    DeviceReplacementRequested = 33,
    DeviceReplacementApproved = 34,
    DeviceReplacementRejected = 35,
    
    // Security Events
    SecurityNewLogin = 40,
    SecurityPasswordChanged = 41,
    SecurityTwoFactorEnabled = 42,
    SecurityTwoFactorDisabled = 43,
    SecurityBackupCodeUsed = 44,
    SecuritySessionTerminated = 45,
    SecuritySuspiciousActivity = 46,
    
    // System Events
    SystemMaintenance = 50,
    SystemUpdate = 51,
    SystemAnnouncement = 52,
    
    // Marketing Events
    PromotionalOffer = 60,
    NewFeature = 61,
    Newsletter = 62,
    
    // Admin Events (for SuperAdmins)
    NewCompanyRegistered = 70,
    PaymentFailed = 71,
    PaymentReceived = 72,
    SupportTicketCreated = 73,
    LicenseKeyGenerated = 74,
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Notification delivery channels
/// </summary>
public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Push = 4,
    Sms = 8,
    All = InApp | Email | Push
}

/// <summary>
/// Target user type for notifications
/// </summary>
public enum NotificationUserType
{
    SuperAdmin = 1,
    CompanyAdmin = 2,
    Both = 3
}
