using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Module;

public class CreateModuleDto
{
    [Required(ErrorMessage = "Module name is required")]
    [StringLength(200, ErrorMessage = "Module name cannot exceed 200 characters")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public string[]? Features { get; set; }

    public bool IsActive { get; set; } = true;
}

