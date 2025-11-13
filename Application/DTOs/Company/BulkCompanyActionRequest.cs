using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Company;

/// <summary>
/// Request DTO for bulk company actions
/// </summary>
public class BulkCompanyActionRequest
{
    /// <summary>
    /// List of encrypted Company IDs
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one company ID is required")]
    public List<Guid> CompanyIds { get; set; } = new();

    /// <summary>
    /// Reason for the bulk action
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes (optional)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to send email notifications to all affected companies
    /// </summary>
    public bool SendEmailNotifications { get; set; } = true;
}
