using System;
using System.Collections.Generic;

namespace Application.DTOs.Licensing;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<ModuleDto> Modules { get; set; } = new List<ModuleDto>();
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

