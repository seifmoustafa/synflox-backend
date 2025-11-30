using System;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Individual result item for bulk access check
/// </summary>
public record AccessCheckResultItem
{
    public Guid? ProjectId { get; init; }
    public Guid? ModuleId { get; init; }
    public string? Feature { get; init; }
    public string? Operation { get; init; }
    public bool HasAccess { get; init; }
    public EntitlementAccessLevel AccessLevel { get; init; }
    public string? DenialReason { get; init; }
}
