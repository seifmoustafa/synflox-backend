using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    public ProjectService(
        IProjectRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        // Check if project with same name exists
        var existing = await _repository.FindAsync(p => p.Name == dto.Name && !p.IsDeleted);
        if (existing.Any())
        {
            throw new BadRequestException(_localizer["Project.NameExists"]);
        }

        var project = _mapper.Map<Project>(dto);
        var created = await _repository.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectDto>(created);
    }

    public async Task<ProjectDto?> UpdateProjectAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _repository.GetByIdAsync(id, null);
        if (project == null || project.IsDeleted)
        {
            return null;
        }

        // Check name uniqueness if name is being updated
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != project.Name)
        {
            var existing = await _repository.FindAsync(p => p.Name == dto.Name && p.Id != id && !p.IsDeleted);
            if (existing.Any())
            {
                throw new BadRequestException(_localizer["Project.NameExists"]);
            }
        }

        _mapper.Map(dto, project);
        await _repository.UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectDto>(project);
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
    {
        var project = await _repository.GetByIdAsync(id, new[] { "ProjectModules.Module" });
        if (project == null || project.IsDeleted)
        {
            return null;
        }

        var dto = _mapper.Map<ProjectDto>(project);
        // Map modules
        if (project.ProjectModules != null && project.ProjectModules.Any())
        {
            var modules = project.ProjectModules
                .Where(pm => !pm.IsDeleted && pm.Module != null)
                .Select(pm => pm.Module!)
                .ToList();
            dto.Modules = _mapper.Map<List<ModuleDto>>(modules);
        }
        return dto;
    }

    public async Task<(IEnumerable<ProjectDto> Projects, PaginationMetadata Meta)> GetAllProjectsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        Expression<Func<Project, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            predicate = p => !p.IsDeleted && (p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        }
        else
        {
            predicate = p => !p.IsDeleted;
        }

        var (entities, meta) = await _repository.GetAllAsync(
            predicate,
            new[] { "ProjectModules.Module" },
            page,
            pageSize,
            search,
            default,
            p => p.Name,
            p => p.Description!);

        // Map projects and their modules
        var dtos = new List<ProjectDto>();
        foreach (var project in entities)
        {
            var dto = _mapper.Map<ProjectDto>(project);
            if (project.ProjectModules != null && project.ProjectModules.Any())
            {
                var modules = project.ProjectModules
                    .Where(pm => !pm.IsDeleted && pm.Module != null)
                    .Select(pm => pm.Module!)
                    .ToList();
                dto.Modules = _mapper.Map<List<ModuleDto>>(modules);
            }
            dtos.Add(dto);
        }

        return (dtos, meta);
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        var project = await _repository.GetByIdAsync(id, null);
        if (project == null || project.IsDeleted)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

