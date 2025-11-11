using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CompanyGroup;

/// <summary>
/// DTO for creating a company group.
/// </summary>
public class CreateCompanyGroupDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}



