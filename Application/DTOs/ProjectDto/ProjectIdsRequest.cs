using System;
using System.Collections.Generic;

namespace Application.DTOs.ProjectDto;

/// <summary>
/// Request wrapper for decrypting a collection of Project IDs
/// </summary>
public class ProjectIdsRequest
{
    public List<Guid> ProjectIds { get; set; } = new();
}
