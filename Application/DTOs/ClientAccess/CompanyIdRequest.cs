using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Request containing a company ID for decryption
/// </summary>
public class CompanyIdRequest
{
    [Required]
    public Guid CompanyId { get; set; }
}
