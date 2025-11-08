using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for updating a company group.
/// </summary>
public class UpdateCompanyGroupDto
{
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}



