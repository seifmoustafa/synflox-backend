using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IPlanProjectModuleRepository _planProjectModuleRepository;
    private readonly IProjectModuleRepository _projectModuleRepository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdEncryptionService _idEncryption;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository repository,
        IPlanProjectModuleRepository planProjectModuleRepository,
        IProjectModuleRepository projectModuleRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        IIdEncryptionService idEncryption)
    {
        _repository = repository;
        _planProjectModuleRepository = planProjectModuleRepository;
        _projectModuleRepository = projectModuleRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _idEncryption = idEncryption;
    }

    public async Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request)
    {
        // Check if plan name already exists
        var existing = await _repository.FindAsync(p => p.Name == request.Name && !p.IsDeleted);
        if (existing.Any())
        {
            throw new BadRequestException(_localizer["SubscriptionPlan.NameExists"]);
        }

        var plan = _mapper.Map<SubscriptionPlan>(request);
        var created = await _repository.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SubscriptionPlanDto>(created);
    }

    public async Task<(IEnumerable<SubscriptionPlanDto> Plans, PaginationMetadata Meta)> GetAllPlansAsync(
        bool? isActive = null,
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        Expression<Func<SubscriptionPlan, object?>>[] searchColumns =
        {
            p => p.Name,
            p => p.Description,
        };

        var (entities, meta) = await _repository.GetAllAsync(null, page, pageSize, search, default, searchColumns);
        var filtered = entities.AsQueryable();

        if (isActive.HasValue)
        {
            filtered = filtered.Where(p => p.IsActive == isActive.Value);
        }

        var plans = filtered.ToList();
        var dtos = _mapper.Map<IEnumerable<SubscriptionPlanDto>>(plans);
        return (dtos, meta);
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id, null);
        if (plan == null || plan.IsDeleted)
        {
            return null;
        }
        return _mapper.Map<SubscriptionPlanDto>(plan);
    }

    public async Task<SubscriptionPlanDto?> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanRequest request)
    {
        var plan = await _repository.GetByIdAsync(id, null);
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        // Check name uniqueness if name is being updated
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != plan.Name)
        {
            var existing = await _repository.FindAsync(p => p.Name == request.Name && p.Id != id && !p.IsDeleted);
            if (existing.Any())
            {
                throw new BadRequestException(_localizer["SubscriptionPlan.NameExists"]);
            }
        }

        _mapper.Map(request, plan);
        await _repository.UpdateAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SubscriptionPlanDto>(plan);
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id, null);
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<SubscriptionPlanDto> UpdatePlanProjectModulesAsync(Guid planId, UpdatePlanProjectModulesDto dto)
    {
        var plan = await _repository.GetByIdAsync(planId, null);
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        // Remove existing plan-project-module relationships
        var existing = await _planProjectModuleRepository.FindAsync(ppm => 
            ppm.SubscriptionPlanId == planId && !ppm.IsDeleted);
        
        foreach (var ppm in existing)
        {
            await _planProjectModuleRepository.DeleteAsync(ppm.Id);
        }

        // Add new relationships
        foreach (var projectModuleId in dto.ProjectModuleIds)
        {
            // Verify project-module exists
            var projectModule = await _projectModuleRepository.GetByIdAsync(projectModuleId, null);
            if (projectModule == null || projectModule.IsDeleted)
            {
                continue; // Skip invalid project-module IDs
            }

            var planProjectModule = new PlanProjectModule
            {
                SubscriptionPlanId = planId,
                ProjectModuleId = projectModuleId,
                IsIncluded = true
            };

            await _planProjectModuleRepository.AddAsync(planProjectModule);
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetPlanByIdAsync(planId) ?? throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
    }

    public async Task<(IEnumerable<PlanProjectModuleDto> PlanProjectModules, PaginationMetadata Meta)> GetPlanProjectModulesAsync(
        Guid planId,
        int page = 1,
        int pageSize = 10)
    {
        Expression<Func<PlanProjectModule, bool>> predicate = ppm => 
            ppm.SubscriptionPlanId == planId && !ppm.IsDeleted;

        var (entities, meta) = await _planProjectModuleRepository.GetAllAsync(
            predicate,
            new[] { "SubscriptionPlan", "ProjectModule.Project", "ProjectModule.Module" },
            page,
            pageSize);

        // All mapping is handled by AutoMapper now
        var dtos = _mapper.Map<List<PlanProjectModuleDto>>(entities);
        return (dtos, meta);
    }
}

