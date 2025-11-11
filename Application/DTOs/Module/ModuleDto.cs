using Application.DTOs.Project;
using System;
using System.Collections.Generic;

namespace Application.DTOs.Module;

public class ModuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[]? Features { get; set; }
    public bool IsActive { get; set; }
    public List<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

