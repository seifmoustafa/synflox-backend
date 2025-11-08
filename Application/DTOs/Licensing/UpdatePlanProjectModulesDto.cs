using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for updating the project-modules included in a subscription plan.
/// </summary>
public class UpdatePlanProjectModulesDto
{
    [Required(ErrorMessage = "Project module IDs are required")]
    public List<Guid> ProjectModuleIds { get; set; } = new List<Guid>();
}

