using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.ProjectModule;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class ProjectModuleService : IProjectModuleService
{
    private readonly IProjectModuleRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    public ProjectModuleService(
        IProjectModuleRepository repository,
        IProjectRepository projectRepository,
        IModuleRepository moduleRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _moduleRepository = moduleRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectModuleDto> CreateProjectModuleAsync(CreateProjectModuleDto dto)
    {
        // Verify project exists
        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, null);
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException(_localizer["Project.NotFound"]);
        }

        // Verify module exists
        var module = await _moduleRepository.GetByIdAsync(dto.ModuleId, null);
        if (module == null || module.IsDeleted)
        {
            throw new NotFoundException(_localizer["Module.NotFound"]);
        }

        // Check if relationship already exists
        var existing = await _repository.FindAsync(pm => 
            pm.ProjectId == dto.ProjectId && 
            pm.ModuleId == dto.ModuleId && 
            !pm.IsDeleted);
        if (existing.Any())
        {
            throw new BadRequestException(_localizer["ProjectModule.AlreadyExists"]);
        }

        var projectModule = _mapper.Map<ProjectModule>(dto);
        var created = await _repository.AddAsync(projectModule);
        await _unitOfWork.SaveChangesAsync();

        // Load with navigation properties for mapping
        var loaded = await _repository.GetByIdAsync(created.Id, new[] { "Project", "Module" });
        return _mapper.Map<ProjectModuleDto>(loaded);
    }

    public async Task<(IEnumerable<ProjectModuleDto> ProjectModules, PaginationMetadata Meta)> GetModulesByProjectIdAsync(
        Guid projectId,
        int page = 1,
        int pageSize = 10)
    {
        Expression<Func<ProjectModule, bool>> predicate = pm => pm.ProjectId == projectId && !pm.IsDeleted;

        var (entities, meta) = await _repository.GetAllAsync(
            predicate,
            new[] { "Project", "Module" },
            page,
            pageSize);

        // All mapping is handled by AutoMapper now
        var dtos = _mapper.Map<List<ProjectModuleDto>>(entities);
        return (dtos, meta);
    }

    public async Task<(IEnumerable<ProjectModuleDto> ProjectModules, PaginationMetadata Meta)> GetProjectsByModuleIdAsync(
        Guid moduleId,
        int page = 1,
        int pageSize = 10)
    {
        Expression<Func<ProjectModule, bool>> predicate = pm => pm.ModuleId == moduleId && !pm.IsDeleted;

        var (entities, meta) = await _repository.GetAllAsync(
            predicate,
            new[] { "Project", "Module" },
            page,
            pageSize);

        // All mapping is handled by AutoMapper now
        var dtos = _mapper.Map<List<ProjectModuleDto>>(entities);
        return (dtos, meta);
    }

    public async Task<bool> DeleteProjectModuleAsync(Guid projectId, Guid moduleId)
    {
        var projectModule = (await _repository.FindAsync(pm => 
            pm.ProjectId == projectId && 
            pm.ModuleId == moduleId && 
            !pm.IsDeleted)).FirstOrDefault();
        
        if (projectModule == null)
        {
            return false;
        }

        await _repository.DeleteAsync(projectModule.Id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

