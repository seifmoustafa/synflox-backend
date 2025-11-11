using System;

namespace Application.DTOs.PlanProjectModule;

public class PlanProjectModuleDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public string SubscriptionPlanName { get; set; } = string.Empty;
    public Guid ProjectModuleId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool IsIncluded { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

