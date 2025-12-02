using System;
using System.Collections.Generic;

namespace Application.DTOs.Common;

/// <summary>
/// Preview of what will be affected by a delete operation
/// </summary>
public class DeletePreviewDto
{
    /// <summary>
    /// The entity being deleted
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the entity being deleted
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// Total count of affected records
    /// </summary>
    public int TotalAffectedCount { get; set; }
    
    /// <summary>
    /// Breakdown of affected records by type
    /// </summary>
    public List<AffectedItemGroup> AffectedItems { get; set; } = new();
    
    /// <summary>
    /// Warning messages to display
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Whether deletion can proceed (no blocking conditions)
    /// </summary>
    public bool CanDelete { get; set; } = true;
    
    /// <summary>
    /// Blocking reason if CanDelete is false
    /// </summary>
    public string? BlockingReason { get; set; }
}

/// <summary>
/// Group of affected items by type
/// </summary>
public class AffectedItemGroup
{
    /// <summary>
    /// Type of affected entity (e.g., "Plans", "Entitlements")
    /// </summary>
    public string ItemType { get; set; } = string.Empty;
    
    /// <summary>
    /// Count of affected items
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// List of affected item names (first 10)
    /// </summary>
    public List<string> ItemNames { get; set; } = new();
}

/// <summary>
/// Request to delete with optional force flag
/// </summary>
public class DeleteWithCascadeRequest
{
    /// <summary>
    /// Encrypted ID of entity to delete
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// If true, cascade delete all related records
    /// If false, returns preview of what would be affected
    /// </summary>
    public bool ConfirmCascade { get; set; } = false;
}
