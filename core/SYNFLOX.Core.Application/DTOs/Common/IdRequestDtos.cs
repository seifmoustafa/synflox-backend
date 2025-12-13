using System;

namespace Application.DTOs.Common;

/// <summary>
/// Request DTO for operations that require a Company ID.
/// Used with AutoMapper converters for ID decryption.
/// </summary>
public class CompanyIdRequest
{
    /// <summary>
    /// The encrypted Company ID from the frontend.
    /// </summary>
    public Guid CompanyId { get; set; }
}

/// <summary>
/// Request DTO for operations that require a Subscription ID.
/// Used with AutoMapper converters for ID decryption.
/// </summary>
public class SubscriptionIdRequest
{
    /// <summary>
    /// The encrypted Subscription ID from the frontend.
    /// </summary>
    public Guid SubscriptionId { get; set; }
}
