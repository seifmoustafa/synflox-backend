using System;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Individual resource check item for bulk access checks
/// </summary>
public record AccessCheckItem
{
    public Guid? ProjectId { get; init; }
    public Guid? ModuleId { get; init; }
    public string? Feature { get; init; }
    public string? Operation { get; init; }
}
