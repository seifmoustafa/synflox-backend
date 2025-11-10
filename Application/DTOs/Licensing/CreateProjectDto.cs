using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class CreateProjectDto
{
    [Required(ErrorMessage = "Project name is required")]
    [StringLength(200, ErrorMessage = "Project name cannot exceed 200 characters")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public string[]? Features { get; set; }

    public bool IsActive { get; set; } = true;
}

