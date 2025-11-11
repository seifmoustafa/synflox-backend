using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ProjectModule;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing project-module relationships.
/// </summary>
public interface IProjectModuleService
{
    /// <summary>
    /// Associates a module with a project.
    /// </summary>
    Task<ProjectModuleDto> CreateProjectModuleAsync(CreateProjectModuleDto dto);

    /// <summary>
    /// Gets all modules for a specific project.
    /// </summary>
    Task<(IEnumerable<ProjectModuleDto> ProjectModules, PaginationMetadata Meta)> GetModulesByProjectIdAsync(
        Guid projectId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets all projects for a specific module.
    /// </summary>
    Task<(IEnumerable<ProjectModuleDto> ProjectModules, PaginationMetadata Meta)> GetProjectsByModuleIdAsync(
        Guid moduleId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Removes a module from a project.
    /// </summary>
    Task<bool> DeleteProjectModuleAsync(Guid projectId, Guid moduleId);
}

