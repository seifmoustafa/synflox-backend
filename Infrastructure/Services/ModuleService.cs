using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ModuleDto;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing Modules (functional sub-units like HR, Accounting, Sales)
/// Single Responsibility: Module management only
/// </summary>
public class ModuleService : IModuleService
{
    private readonly IModuleRepository _moduleRepo;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public ModuleService(
        IModuleRepository moduleRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _moduleRepo = moduleRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<ModuleDto> CreateAsync(CreateModuleDto dto)
    {
        var existing = await _moduleRepo.GetByNameAsync(dto.Name);
        if (existing != null)
            throw new BadRequestException(_localizer["Module.NameExists"]);

        var module = _mapper.Map<Module>(dto);
        module.Id = Guid.NewGuid();

        await _moduleRepo.AddAsync(module);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ModuleDto>(module);
    }

    public async Task<ModuleDto?> GetByIdAsync(ModuleIdRequest request)
    {
        // Use AutoMapper to decrypt the encrypted Module ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        // Include ProjectModules.Project for bidirectional integration
        var module = await _moduleRepo.GetByIdAsync(decryptedId, new[] { "ProjectModules.Project" });
        return module == null ? null : _mapper.Map<ModuleDto>(module);
    }

    public async Task<(IEnumerable<ModuleDto> Modules, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (modules, meta) = await _moduleRepo.GetAllAsync(
            null,
            page,
            pageSize,
            search,
            default,
            m => m.Name);

        var dtos = _mapper.Map<IEnumerable<ModuleDto>>(modules);
        return (dtos, meta);
    }

    public async Task<ModuleDto?> UpdateAsync(ModuleIdRequest request, UpdateModuleDto dto)
    {
        // Use AutoMapper to decrypt the encrypted Module ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        var module = await _moduleRepo.GetByIdAsync(decryptedId, null);
        if (module == null)
            throw new NotFoundException(_localizer["Module.NotFound"]);

        if (dto.Name != null && dto.Name != module.Name)
        {
            var existing = await _moduleRepo.GetByNameAsync(dto.Name);
            if (existing != null && existing.Id != decryptedId)
                throw new BadRequestException(_localizer["Module.NameExists"]);
        }

        _mapper.Map(dto, module);
        await _moduleRepo.UpdateAsync(module);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ModuleDto>(module);
    }

    public async Task<bool> DeleteAsync(ModuleIdRequest request)
    {
        // Use AutoMapper to decrypt the encrypted Module ID (SYNFLOX ID encryption rule)
        var decryptedId = _mapper.Map<Guid>(request);
        
        var module = await _moduleRepo.GetByIdAsync(decryptedId, null);
        if (module == null)
            throw new NotFoundException(_localizer["Module.NotFound"]);

        // Check if module is used in projects or plans
        var isInUse = await _moduleRepo.IsUsedInProjectsOrPlansAsync(decryptedId);
        if (isInUse)
            throw new InvalidOperationException(_localizer["Module.InUse"]);

        await _moduleRepo.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task ActivateAsync(ModuleIdRequest request)
    {
        // Decrypt ID via AutoMapper (SYNFLOX rule)
        var decryptedId = _mapper.Map<Guid>(request);

        var module = await _moduleRepo.GetByIdAsync(decryptedId, null);
        if (module == null)
            throw new KeyNotFoundException(_localizer["Module.NotFound"]);

        module.IsActive = true;
        await _moduleRepo.UpdateAsync(module);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateAsync(ModuleIdRequest request)
    {
        // Decrypt ID via AutoMapper (SYNFLOX rule)
        var decryptedId = _mapper.Map<Guid>(request);

        var module = await _moduleRepo.GetByIdAsync(decryptedId, null);
        if (module == null)
            throw new KeyNotFoundException(_localizer["Module.NotFound"]);

        module.IsActive = false;
        await _moduleRepo.UpdateAsync(module);
        await _unitOfWork.SaveChangesAsync();
    }
}
