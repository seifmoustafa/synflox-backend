using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class UpdateModuleDto
{
    [StringLength(200, ErrorMessage = "Module name cannot exceed 200 characters")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

