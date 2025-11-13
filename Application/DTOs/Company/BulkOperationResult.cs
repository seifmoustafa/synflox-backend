using System;
using System.Collections.Generic;

namespace Application.DTOs.Company;

/// <summary>
/// Result DTO for bulk operations
/// </summary>
public class BulkOperationResult
{
    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// List of successful company IDs (encrypted)
    /// </summary>
    public List<Guid> SuccessfulIds { get; set; } = new();

    /// <summary>
    /// List of failed operations with error messages
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();

    /// <summary>
    /// Overall operation success status
    /// </summary>
    public bool IsSuccess => FailureCount == 0;

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary => $"Processed {TotalProcessed} items: {SuccessCount} successful, {FailureCount} failed";
}

/// <summary>
/// Error details for failed bulk operations
/// </summary>
public class BulkOperationError
{
    /// <summary>
    /// Company ID that failed (encrypted)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company name (for display purposes)
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
