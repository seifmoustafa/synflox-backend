using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing projects (e.g., ERP, CRM, POS).
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project.
    /// </summary>
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    Task<ProjectDto?> UpdateProjectAsync(Guid id, UpdateProjectDto dto);

    /// <summary>
    /// Gets a project by ID.
    /// </summary>
    Task<ProjectDto?> GetProjectByIdAsync(Guid id);

    /// <summary>
    /// Gets all projects with pagination.
    /// </summary>
    Task<(IEnumerable<ProjectDto> Projects, PaginationMetadata Meta)> GetAllProjectsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null);

    /// <summary>
    /// Deletes a project (soft delete).
    /// </summary>
    Task<bool> DeleteProjectAsync(Guid id);
}

