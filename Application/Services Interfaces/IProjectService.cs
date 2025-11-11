using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Project management
/// Single Responsibility: Projects only
/// </summary>
public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<ProjectDto> Projects, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectDto dto);
    Task<bool> DeleteAsync(Guid id);
}
