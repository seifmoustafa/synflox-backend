using System;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Simplified Project DTO for module relationships
/// Contains only essential fields to avoid circular references
/// </summary>
public class SimpleProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
