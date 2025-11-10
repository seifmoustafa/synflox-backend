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

public class ModuleService : IModuleService
{
    private readonly IModuleRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    public ModuleService(
        IModuleRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ModuleDto> CreateModuleAsync(CreateModuleDto dto)
    {
        // Check if module with same name exists
        var existing = await _repository.FindAsync(m => m.Name == dto.Name && !m.IsDeleted);
        if (existing.Any())
        {
            throw new BadRequestException(_localizer["Module.NameExists"]);
        }

        var module = _mapper.Map<Module>(dto);
        var created = await _repository.AddAsync(module);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ModuleDto>(created);
    }

    public async Task<ModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleDto dto)
    {
        var module = await _repository.GetByIdAsync(id, null);
        if (module == null || module.IsDeleted)
        {
            return null;
        }

        // Check name uniqueness if name is being updated
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != module.Name)
        {
            var existing = await _repository.FindAsync(m => m.Name == dto.Name && m.Id != id && !m.IsDeleted);
            if (existing.Any())
            {
                throw new BadRequestException(_localizer["Module.NameExists"]);
            }
        }

        _mapper.Map(dto, module);
        await _repository.UpdateAsync(module);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ModuleDto>(module);
    }

    public async Task<ModuleDto?> GetModuleByIdAsync(Guid id)
    {
        var module = await _repository.GetByIdAsync(id, new[] { "ProjectModules.Project" });
        if (module == null || module.IsDeleted)
        {
            return null;
        }

        var dto = _mapper.Map<ModuleDto>(module);
        // Map projects
        if (module.ProjectModules != null && module.ProjectModules.Any())
        {
            var projects = module.ProjectModules
                .Where(pm => !pm.IsDeleted && pm.Project != null)
                .Select(pm => pm.Project!)
                .ToList();
            dto.Projects = _mapper.Map<List<ProjectDto>>(projects);
        }
        return dto;
    }

    public async Task<(IEnumerable<ModuleDto> Modules, PaginationMetadata Meta)> GetAllModulesAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        Expression<Func<Module, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(search))
        {
            predicate = m => !m.IsDeleted
                             && (
                                 // Self fields
                                 m.Name.Contains(search)
                                 || (m.Description != null && m.Description.Contains(search))
                                 // Related: Projects via ProjectModules
                                 || m.ProjectModules.Any(pm =>
                                        !pm.IsDeleted
                                        && pm.Project != null
                                        && (
                                            pm.Project.Name.Contains(search)
                                            || (pm.Project.Description != null && pm.Project.Description.Contains(search))
                                        ))
                             );
        }
        else
        {
            predicate = m => !m.IsDeleted;
        }

        var (entities, meta) = await _repository.GetAllAsync(
            predicate,
            new[] { "ProjectModules.Project" },
            page,
            pageSize,
            search,
            default,
            m => m.Name,
            m => m.Description!);

        // Map modules and their projects
        var dtos = new List<ModuleDto>();
        foreach (var module in entities)
        {
            var dto = _mapper.Map<ModuleDto>(module);
            if (module.ProjectModules != null && module.ProjectModules.Any())
            {
                var projects = module.ProjectModules
                    .Where(pm => !pm.IsDeleted && pm.Project != null)
                    .Select(pm => pm.Project!)
                    .ToList();
                dto.Projects = _mapper.Map<List<ProjectDto>>(projects);
            }
            dtos.Add(dto);
        }

        return (dtos, meta);
    }

    public async Task<bool> DeleteModuleAsync(Guid id)
    {
        var module = await _repository.GetByIdAsync(id, null);
        if (module == null || module.IsDeleted)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

