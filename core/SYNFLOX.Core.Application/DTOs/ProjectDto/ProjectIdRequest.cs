using System;

namespace Application.DTOs.ProjectDto;

/// <summary>
/// Request DTO for operations requiring a Project ID
/// ID will be decrypted by AutoMapper (SYNFLOX ID encryption rule compliance)
/// </summary>
public class ProjectIdRequest
{
    public Guid ProjectId { get; set; }
}
