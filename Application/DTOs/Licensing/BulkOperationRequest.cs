using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for bulk operations on companies.
/// </summary>
public class BulkOperationRequest
{
    [Required(ErrorMessage = "Company IDs are required")]
    [MinLength(1, ErrorMessage = "At least one company ID is required")]
    public List<Guid> CompanyIds { get; set; } = new List<Guid>();

    [Required(ErrorMessage = "Action is required")]
    public BulkOperationAction Action { get; set; }

    /// <summary>
    /// Required for Extend action. The new expiry date for all companies.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Types of bulk operations.
/// </summary>
public enum BulkOperationAction
{
    Activate = 1,
    Suspend = 2,
    Resume = 3,
    Extend = 4,
    Delete = 5
}

