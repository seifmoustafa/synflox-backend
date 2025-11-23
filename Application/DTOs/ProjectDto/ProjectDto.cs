using Application.DTOs.Subscriptions;
using System;
using System.Collections.Generic;

namespace Application.DTOs.ProjectDto;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Features { get; set; } = new();
    public List<ModuleDto> Modules { get; set; } = new();
}
