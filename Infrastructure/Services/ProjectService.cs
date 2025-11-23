using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.ProjectDto;
using Application.DTOs.Subscriptions;
using Application.DTOs.ModuleDto;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing Projects (top-level products like ERP, CRM, POS)
/// Single Responsibility: Project management only
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IModuleRepository _moduleRepo;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(
        IProjectRepository projectRepo,
        IModuleRepository moduleRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _projectRepo = projectRepo;
        _moduleRepo = moduleRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        // Check unique name
        var existing = await _projectRepo.GetByNameAsync(dto.Name);
        if (existing != null)
            throw new BadRequestException(_localizer["Project.NameExists"]);

        var project = _mapper.Map<Project>(dto);
        project.Id = Guid.NewGuid();

        // Associate modules - decrypt ModuleIds using mapper
        if (dto.ModuleIds.Any())
        {
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            foreach (var moduleId in decryptedModuleIds)
            {
                var module = await _moduleRepo.GetByIdAsync(moduleId, null);
                if (module == null)
                    throw new NotFoundException(_localizer["Module.NotFound"]);

                project.ProjectModules.Add(new ProjectModule
                {
                    ProjectId = project.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _projectRepo.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        var result = await _projectRepo.GetByIdWithModulesAsync(project.Id);
        return _mapper.Map<ProjectDto>(result!);
    }

    public async Task<ProjectDto?> GetByIdAsync(ProjectIdRequest request)
    {
        // Use AutoMapper to decrypt the ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        var project = await _projectRepo.GetByIdWithModulesAsync(decryptedId);
        return project == null ? null : _mapper.Map<ProjectDto>(project);
    }

    public async Task<(IEnumerable<ProjectDto> Projects, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (projects, meta) = await _projectRepo.GetAllAsync(
            new[] { "ProjectModules.Module" },
            page,
            pageSize,
            search,
            default,
            p => p.Name);

        var dtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);
        return (dtos, meta);
    }

    public async Task<ProjectDto?> UpdateAsync(ProjectIdRequest request, UpdateProjectDto dto)
    {
        // Use AutoMapper to decrypt the ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        var project = await _projectRepo.GetByIdAsync(decryptedId, new[] { "ProjectModules" });
        if (project == null)
            throw new NotFoundException(_localizer["Project.NotFound"]);

        // Check unique name if changing
        if (dto.Name != null && dto.Name != project.Name)
        {
            var existing = await _projectRepo.GetByNameAsync(dto.Name);
            if (existing != null && existing.Id != decryptedId)
                throw new BadRequestException(_localizer["Project.NameExists"]);
        }

        _mapper.Map(dto, project);

        // Update module associations - decrypt ModuleIds using mapper
        if (dto.ModuleIds != null)
        {
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            project.ProjectModules.Clear();
            foreach (var moduleId in decryptedModuleIds)
            {
                var module = await _moduleRepo.GetByIdAsync(moduleId, null);
                if (module == null)
                    throw new NotFoundException(_localizer["Module.NotFound"]);

                project.ProjectModules.Add(new ProjectModule
                {
                    ProjectId = project.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _projectRepo.UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();

        var result = await _projectRepo.GetByIdWithModulesAsync(decryptedId);
        return _mapper.Map<ProjectDto>(result);
    }

    public async Task<bool> DeleteAsync(ProjectIdRequest request)
    {
        // Use AutoMapper to decrypt the ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        var project = await _projectRepo.GetByIdAsync(decryptedId, null);
        if (project == null)
            throw new NotFoundException(_localizer["Project.NotFound"]);

        // Check if project is used in any active subscriptions
        var isInUse = await _projectRepo.IsUsedInActiveSubscriptionsAsync(decryptedId);
        if (isInUse)
            throw new InvalidOperationException(_localizer["Project.InUse"]);

        await _projectRepo.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
