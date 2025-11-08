using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for bulk update operations on companies.
/// </summary>
public class BulkUpdateRequest
{
    [Required(ErrorMessage = "Company IDs are required")]
    [MinLength(1, ErrorMessage = "At least one company ID is required")]
    public List<Guid> CompanyIds { get; set; } = new List<Guid>();

    [Required(ErrorMessage = "Update DTO is required")]
    public UpdateCompanyDto UpdateDto { get; set; } = null!;
}

