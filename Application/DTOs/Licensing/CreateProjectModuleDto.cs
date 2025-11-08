using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class CreateProjectModuleDto
{
    [Required(ErrorMessage = "Project ID is required")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "Module ID is required")]
    public Guid ModuleId { get; set; }

    public bool IsEnabledByDefault { get; set; } = true;
}

