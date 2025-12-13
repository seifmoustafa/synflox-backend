using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Common;
using Application.DTOs.ProjectDto;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Project management
/// Single Responsibility: Projects only
/// </summary>
public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto?> GetByIdAsync(ProjectIdRequest request);
    Task<(IEnumerable<ProjectDto> Projects, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<ProjectDto?> UpdateAsync(ProjectIdRequest request, UpdateProjectDto dto);
    
    /// <summary>
    /// Preview what will be affected by deleting a project
    /// </summary>
    Task<DeletePreviewDto> GetDeletePreviewAsync(ProjectIdRequest request);
    
    /// <summary>
    /// Delete a project with optional cascade
    /// If confirmCascade is false, returns preview
    /// If confirmCascade is true, deletes project and all related records
    /// </summary>
    Task<bool> DeleteAsync(ProjectIdRequest request, bool confirmCascade = false);
}
