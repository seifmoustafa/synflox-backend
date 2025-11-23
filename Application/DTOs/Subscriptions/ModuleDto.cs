using System;
using System.Collections.Generic;

namespace Application.DTOs.Subscriptions;

public class ModuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<string> Features { get; set; } = new();
    // Removed Projects list - modules don't own the relationship, projects do!
}
