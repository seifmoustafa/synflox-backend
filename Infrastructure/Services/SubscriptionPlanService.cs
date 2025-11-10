using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
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

    public SubscriptionPlanService(
        ISubscriptionPlanRepository repository,
        IPlanProjectModuleRepository planProjectModuleRepository,
        IProjectModuleRepository projectModuleRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _planProjectModuleRepository = planProjectModuleRepository;
        _projectModuleRepository = projectModuleRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
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

        // Collect all project-module ids to add (from ProjectIds and explicit ProjectModuleIds)
        var toAdd = new HashSet<Guid>();

        // From complete projects: include all their modules
        if (dto.ProjectIds != null && dto.ProjectIds.Count > 0)
        {
            foreach (var projectId in dto.ProjectIds)
            {
                var pms = await _projectModuleRepository.FindAsync(pm => pm.ProjectId == projectId && !pm.IsDeleted);
                foreach (var pm in pms)
                {
                    toAdd.Add(pm.Id);
                }
            }
        }

        // From explicit project-module ids
        if (dto.ProjectModuleIds != null && dto.ProjectModuleIds.Count > 0)
        {
            foreach (var id in dto.ProjectModuleIds)
                toAdd.Add(id);
        }

        // Add new relationships
        foreach (var projectModuleId in toAdd)
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

    public async Task<PlanFeaturesDto> GetPlanFeaturesWithInheritanceAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId, new[] { "ParentPlan", "ChildPlans", "PlanProjectModules.ProjectModule.Project", "PlanProjectModules.ProjectModule.Module" });
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        var result = _mapper.Map<PlanFeaturesDto>(plan);
        result.OwnFeatures = GetPlanFeatures(plan);
        result.ProjectModules = _mapper.Map<List<PlanProjectModuleDto>>(plan.PlanProjectModules.Where(ppm => !ppm.IsDeleted));

        // Get inherited features from parent plans
        var allFeatures = new List<string>(result.OwnFeatures);
        var inheritedProjectModules = new List<PlanProjectModuleDto>();
        
        var currentPlan = plan.ParentPlan;
        while (currentPlan != null && !currentPlan.IsDeleted)
        {
            // Add parent features
            var parentFeatures = GetPlanFeatures(currentPlan);
            allFeatures.AddRange(parentFeatures);
            
            // Add parent project modules
            var (parentModules, _) = await _planProjectModuleRepository.GetAllAsync(
                ppm => ppm.SubscriptionPlanId == currentPlan.Id && !ppm.IsDeleted,
                new[] { "ProjectModule.Project", "ProjectModule.Module" },
                1, int.MaxValue);
            inheritedProjectModules.AddRange(_mapper.Map<List<PlanProjectModuleDto>>(parentModules));
            
            // Move to next parent
            currentPlan = await _repository.GetByIdAsync(currentPlan.Id, new[] { "ParentPlan" }, CancellationToken.None);
            currentPlan = currentPlan?.ParentPlan;
        }

        result.AllFeatures = allFeatures.Distinct().ToArray();
        result.InheritedProjectModules = inheritedProjectModules;
        result.ParentPlan = plan.ParentPlan != null ? _mapper.Map<SubscriptionPlanDto>(plan.ParentPlan) : null;
        result.ChildPlans = _mapper.Map<List<SubscriptionPlanDto>>(plan.ChildPlans.Where(cp => !cp.IsDeleted));

        return result;
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetUpgradePathAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId, null, CancellationToken.None);
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        // Get all plans with higher tiers that are active
        var upgradePlans = await _repository.FindAsync(
            p => p.PlanTier > plan.PlanTier && p.IsActive && !p.IsDeleted);

        return _mapper.Map<List<SubscriptionPlanDto>>(upgradePlans.OrderBy(p => p.PlanTier));
    }

    public async Task<SubscriptionPlanDto> SetPlanParentAsync(Guid planId, Guid? parentPlanId)
    {
        var plan = await _repository.GetByIdAsync(planId, null, CancellationToken.None);
        if (plan == null || plan.IsDeleted)
        {
            throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
        }

        // Validate parent plan if provided
        if (parentPlanId.HasValue)
        {
            var parentPlan = await _repository.GetByIdAsync(parentPlanId.Value, null, CancellationToken.None);
            if (parentPlan == null || parentPlan.IsDeleted)
            {
                throw new NotFoundException(_localizer["SubscriptionPlan.ParentNotFound"]);
            }

            // Prevent circular references
            if (await WouldCreateCircularReference(planId, parentPlanId.Value))
            {
                throw new InvalidOperationException(_localizer["SubscriptionPlan.CircularReferenceError"]);
            }
        }

        plan.ParentPlanId = parentPlanId;
        await _repository.UpdateAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return await GetPlanByIdAsync(planId) ?? throw new NotFoundException(_localizer["SubscriptionPlan.NotFound"]);
    }

    private static string[] GetPlanFeatures(SubscriptionPlan plan)
    {
        if (string.IsNullOrEmpty(plan.Features))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<string[]>(plan.Features) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private async Task<bool> WouldCreateCircularReference(Guid planId, Guid parentPlanId)
    {
        var visited = new HashSet<Guid>();
        var currentId = parentPlanId;

        while (currentId != Guid.Empty && !visited.Contains(currentId))
        {
            if (currentId == planId)
                return true;

            visited.Add(currentId);
            
            var currentPlan = await _repository.GetByIdAsync(currentId, null, CancellationToken.None);
            currentId = currentPlan?.ParentPlanId ?? Guid.Empty;
        }

        return false;
    }
}

