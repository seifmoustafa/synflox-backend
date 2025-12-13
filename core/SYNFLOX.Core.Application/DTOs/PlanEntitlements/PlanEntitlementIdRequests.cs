using System;

namespace Application.DTOs.PlanEntitlements;

/// <summary>
/// Request DTO for decrypting a plan entitlement ID
/// </summary>
public class PlanEntitlementIdRequest
{
    public Guid EntitlementId { get; set; }
}

/// <summary>
/// Request DTO for decrypting a plan ID for entitlement operations
/// </summary>
public class PlanIdForEntitlementRequest
{
    public Guid PlanId { get; set; }
}

/// <summary>
/// Request DTO for copying entitlements between plans
/// </summary>
public class CopyEntitlementsRequest
{
    public Guid SourcePlanId { get; set; }
    public Guid TargetPlanId { get; set; }
}
