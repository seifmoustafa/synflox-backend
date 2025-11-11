using System;

namespace Application.DTOs.ProjectModule;

public class ProjectModuleDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool IsEnabledByDefault { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

