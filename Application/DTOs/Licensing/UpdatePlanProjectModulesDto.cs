using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// DTO for updating the project-modules included in a subscription plan.
/// </summary>
public class UpdatePlanProjectModulesDto
{
    /// <summary>
    /// Optional: Include complete projects (all their modules will be added to the plan).
    /// </summary>
    public List<Guid> ProjectIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Optional: Include specific project-module links explicitly.
    /// </summary>
    public List<Guid> ProjectModuleIds { get; set; } = new List<Guid>();
}

