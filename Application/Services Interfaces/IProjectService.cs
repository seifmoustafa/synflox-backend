using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    Task<bool> DeleteAsync(ProjectIdRequest request);
}
