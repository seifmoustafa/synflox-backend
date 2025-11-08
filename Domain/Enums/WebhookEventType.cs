namespace Domain.Enums;

/// <summary>
/// Represents the type of webhook event.
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Company subscription was activated.
    /// </summary>
    CompanyActivated = 1,

    /// <summary>
    /// Company subscription was suspended.
    /// </summary>
    CompanySuspended = 2,

    /// <summary>
    /// Company subscription was resumed.
    /// </summary>
    CompanyResumed = 3,

    /// <summary>
    /// Company subscription was extended.
    /// </summary>
    CompanyExtended = 4,

    /// <summary>
    /// Company subscription expired.
    /// </summary>
    CompanyExpired = 5,

    /// <summary>
    /// Company was created.
    /// </summary>
    CompanyCreated = 6,

    /// <summary>
    /// Company was updated.
    /// </summary>
    CompanyUpdated = 7,

    /// <summary>
    /// Company was deleted.
    /// </summary>
    CompanyDeleted = 8
}

