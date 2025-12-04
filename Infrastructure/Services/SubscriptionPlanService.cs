using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.PlanDto;
using Application.DTOs.Subscriptions;
using Application.DTOs.ProjectDto;
using Application.DTOs.ModuleDto;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing Subscription Plans (commercial offerings like Basic, Pro, Enterprise)
/// Single Responsibility: Subscription Plan management only
/// </summary>
public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IModuleRepository _moduleRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPlanEntitlementRepository _entitlementRepo;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository planRepo,
        IProjectRepository projectRepo,
        IModuleRepository moduleRepo,
        ISubscriptionRepository subscriptionRepo,
        IPlanEntitlementRepository entitlementRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _planRepo = planRepo;
        _projectRepo = projectRepo;
        _moduleRepo = moduleRepo;
        _subscriptionRepo = subscriptionRepo;
        _entitlementRepo = entitlementRepo;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanDto> CreateAsync(CreateSubscriptionPlanDto dto)
    {
        return await CreateAsyncInternal(dto, skipModuleValidation: false);
    }
    
    /// <summary>
    /// Internal create method with option to skip module conflict validation
    /// </summary>
    private async Task<PlanDto> CreateAsyncInternal(CreateSubscriptionPlanDto dto, bool skipModuleValidation)
    {
        // ⭐ FREE TIER PLAN VALIDATION - Must be first (overrides everything)
        if (dto.IsFreeTier)
        {
            // Free tier plans MUST be lifetime (never expire)
            dto.DurationType = Domain.Enums.PlanDurationType.Lifetime;
            // Free tier doesn't need trial
            dto.AllowTrial = false;
            dto.TrialDurationDays = null;
            // Free tier doesn't renew (lifetime)
            dto.AutoRenew = false;
            // Free tier uses FullReplace
            dto.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            // Free tier has no grace period (doesn't expire)
            dto.GracePeriodDays = 0;
            // Free tier has no export grace (doesn't get blocked)
            dto.ExportGraceDays = 0;
            // Free tier IS the fallback - cannot have a fallback itself
            dto.DefaultFallbackPlanId = null;
            // Free tier has full access to its modules
            dto.FallbackAccessMode = Domain.Enums.SubscriptionAccessMode.Full;
            // Free tier has Free currency with 0 amount (auto-set, ignore any prices sent)
            dto.Prices = new List<PlanPriceDto>
            {
                new PlanPriceDto { Currency = Domain.Enums.Currency.Free, Amount = 0 }
            };
        }
        
        // ⭐ LIFETIME PLAN VALIDATION
        if (dto.DurationType == Domain.Enums.PlanDurationType.Lifetime)
        {
            // Reject invalid values for Lifetime plans
            if (dto.AllowTrial)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotHaveTrial"]);
            
            if (dto.AutoRenew)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotAutoRenew"]);
            
            // Lifetime plans can only use FullReplace upgrade policy
            if (dto.UpgradePolicy != Domain.Enums.UpgradePolicy.FullReplace)
                throw new BadRequestException(_localizer["Plan.LifetimeMustUseFullReplace"]);
            
            // Force grace period to 0 (lifetime never expires)
            dto.GracePeriodDays = 0;
        }
        
        // ⭐ FALLBACK PLAN VALIDATION
        if (dto.DefaultFallbackPlanId.HasValue)
        {
            var fallbackPlan = await _planRepo.GetByIdAsync(dto.DefaultFallbackPlanId.Value, null);
            if (fallbackPlan == null)
                throw new NotFoundException(_localizer["Plan.FallbackNotFound"]);
            
            if (!fallbackPlan.IsFreeTier)
                throw new BadRequestException(_localizer["Plan.FallbackMustBeFreeTier"]);
        }
        
        // ⭐ VALIDATE - All cases handled
        
        // Trial duration is required only if trial is enabled (and not free tier/lifetime)
        if (dto.AllowTrial && (!dto.TrialDurationDays.HasValue || dto.TrialDurationDays.Value <= 0))
            throw new BadRequestException(_localizer["Plan.TrialDurationRequired"]);

        // Prices required only for PAID plans (Free Tier already has auto-set price)
        if (!dto.IsFreeTier && !dto.Prices.Any())
            throw new BadRequestException(_localizer["Plan.AtLeastOnePriceRequired"]);

        // Must include at least one project or module (unless inheriting from parent)
        if (!dto.ProjectIds.Any() && !dto.ModuleIds.Any() && !dto.ParentPlanId.HasValue)
            throw new BadRequestException(_localizer["Plan.MustIncludeContent"]);
        
        // ⭐ MODULE CONFLICT VALIDATION - Check for modules already in projects (CREATE)
        // Skip if called from CreateWithConfirmationAsync (already validated and filtered)
        if (!skipModuleValidation && dto.ProjectIds.Any() && dto.ModuleIds.Any())
        {
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            var validation = await ValidatePlanModulesAsync(new ValidatePlanModulesRequest
            {
                PlanId = null, // New plan
                ProjectIds = decryptedProjectIds,
                ModuleIds = decryptedModuleIds
            });
            
            if (validation.HasWarnings)
            {
                // Throw exception - frontend must use CreateWithConfirmationAsync
                throw new PlanModuleConflictException(validation);
            }
        }

        // ⭐ PLAN HIERARCHY VALIDATION
        HashSet<Guid> inheritedProjectIds = new();
        HashSet<Guid> inheritedModuleIds = new();
        
        if (dto.ParentPlanId.HasValue)
        {
            var parentPlan = await _planRepo.GetWithDetailsAsync(dto.ParentPlanId.Value);
            if (parentPlan == null)
                throw new NotFoundException(_localizer["Plan.ParentNotFound"]);
            
            // Collect all inherited projects and modules from parent chain
            await CollectInheritedFeaturesAsync(parentPlan, inheritedProjectIds, inheritedModuleIds);
            
            // Validate no duplicate projects
            var decryptedProjectIds = dto.ProjectIds.Any() 
                ? _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList()
                : new List<Guid>();
            var duplicateProjects = decryptedProjectIds.Where(id => inheritedProjectIds.Contains(id)).ToList();
            if (duplicateProjects.Any())
                throw new BadRequestException(_localizer["Plan.ProjectAlreadyInherited"]);
            
            // Validate no duplicate modules
            var decryptedModuleIds = dto.ModuleIds.Any()
                ? _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList()
                : new List<Guid>();
            var duplicateModules = decryptedModuleIds.Where(id => inheritedModuleIds.Contains(id)).ToList();
            if (duplicateModules.Any())
                throw new BadRequestException(_localizer["Plan.ModuleAlreadyInherited"]);
        }

        var existing = await _planRepo.GetByNameAsync(dto.Name);
        if (existing != null)
            throw new BadRequestException(_localizer["Plan.NameExists"]);

        var plan = _mapper.Map<SubscriptionPlan>(dto);
        plan.Id = Guid.NewGuid();
        
        // ⭐ FORCE correct values for Free Tier (defense in depth)
        if (plan.IsFreeTier)
        {
            plan.DurationType = Domain.Enums.PlanDurationType.Lifetime;
            plan.AllowTrial = false;
            plan.TrialDurationDays = null;
            plan.AutoRenew = false;
            plan.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            plan.GracePeriodDays = 0;
            plan.ExportGraceDays = 0;
            plan.DefaultFallbackPlanId = null;
            plan.FallbackAccessMode = Domain.Enums.SubscriptionAccessMode.Full;
        }
        // ⭐ FORCE correct values for Lifetime (defense in depth)
        else if (plan.DurationType == Domain.Enums.PlanDurationType.Lifetime)
        {
            plan.AllowTrial = false;
            plan.AutoRenew = false;
            plan.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            plan.GracePeriodDays = 0;
        }

        // Add prices
        foreach (var priceDto in dto.Prices)
        {
            plan.PlanPrices.Add(new PlanPrice
            {
                PlanId = plan.Id,
                Currency = priceDto.Currency,
                Amount = priceDto.Amount
            });
        }

        // Add projects - decrypt ProjectIds using mapper
        if (dto.ProjectIds.Any())
        {
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            
            foreach (var projectId in decryptedProjectIds)
            {
                var project = await _projectRepo.GetByIdAsync(projectId, null);
                if (project == null)
                    throw new NotFoundException(_localizer["Project.NotFound"]);

                plan.PlanProjects.Add(new PlanProject
                {
                    PlanId = plan.Id,
                    ProjectId = projectId
                });
            }
        }

        // Add modules - decrypt ModuleIds using mapper
        if (dto.ModuleIds.Any())
        {
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            foreach (var moduleId in decryptedModuleIds)
            {
                var module = await _moduleRepo.GetByIdAsync(moduleId, null);
                if (module == null)
                    throw new NotFoundException(_localizer["Module.NotFound"]);

                plan.PlanModules.Add(new PlanModule
                {
                    PlanId = plan.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _planRepo.AddAsync(plan);
        
        // ⭐ AUTO-CREATE ENTITLEMENTS with Full Access for all included projects/modules
        await CreateEntitlementsForPlanAsync(plan);
        
        await _unitOfWork.SaveChangesAsync();

        var result = await _planRepo.GetWithDetailsAsync(plan.Id);
        return _mapper.Map<PlanDto>(result!);
    }

    public async Task<PlanDto?> GetByIdAsync(PlanIdRequest request)
    {
        // Decrypt Plan ID using AutoMapper (SYNFLOX ID encryption rule compliance)
        var decryptedPlanId = _mapper.Map<Guid>(request);
        
        var plan = await _planRepo.GetWithDetailsAsync(decryptedPlanId);
        return plan == null ? null : _mapper.Map<PlanDto>(plan);
    }

    public async Task<PlanDetailsDto?> GetDetailsAsync(PlanIdRequest request)
    {
        // Decrypt Plan ID using AutoMapper (SYNFLOX ID encryption rule compliance)
        var decryptedPlanId = _mapper.Map<Guid>(request);
        
        var plan = await _planRepo.GetWithDetailsAsync(decryptedPlanId);
        return plan == null ? null : _mapper.Map<PlanDetailsDto>(plan);
    }

    public async Task<(IEnumerable<PlanDto> Plans, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (plans, meta) = await _planRepo.GetAllAsync(
            new[] { 
                "PlanPrices", 
                "PlanProjects.Project.ProjectModules",  // Need Project and its modules for filtering
                "PlanModules.Module",  // Need Module entity for mapping
                "ParentPlan", 
                "DefaultFallbackPlan",
                "ChildPlans"  // Need for ChildPlanCount
            },
            page,
            pageSize,
            search,
            default,
            p => p.Name);

        var dtos = _mapper.Map<IEnumerable<PlanDto>>(plans);
        return (dtos, meta);
    }

    public async Task<PlanDto?> UpdateAsync(PlanIdRequest request, UpdatePlanDto dto)
    {
        return await UpdateAsyncInternal(request, dto, skipModuleValidation: false);
    }
    
    /// <summary>
    /// Internal update method with option to skip module conflict validation
    /// </summary>
    private async Task<PlanDto?> UpdateAsyncInternal(PlanIdRequest request, UpdatePlanDto dto, bool skipModuleValidation)
    {
        // Decrypt Plan ID using AutoMapper (SYNFLOX ID encryption rule compliance)
        var decryptedPlanId = _mapper.Map<Guid>(request);
        
        var plan = await _planRepo.GetByIdAsync(decryptedPlanId, new[] { "PlanPrices", "PlanProjects", "PlanModules" });
        if (plan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // ⭐ FREE TIER PLAN VALIDATION for UPDATE - Must be first
        var isFreeTier = dto.IsFreeTier ?? plan.IsFreeTier;
        if (isFreeTier)
        {
            // Free tier plans MUST be lifetime
            dto.DurationType = Domain.Enums.PlanDurationType.Lifetime;
            dto.AllowTrial = false;
            dto.TrialDurationDays = null;
            dto.AutoRenew = false;
            dto.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            dto.GracePeriodDays = 0;
            dto.ExportGraceDays = 0;
            dto.DefaultFallbackPlanId = null;
            dto.FallbackAccessMode = Domain.Enums.SubscriptionAccessMode.Full;
            // Free tier has Free currency with 0 amount (auto-set)
            dto.Prices = new List<PlanPriceDto>
            {
                new PlanPriceDto { Currency = Domain.Enums.Currency.Free, Amount = 0 }
            };
        }

        // ⭐ FALLBACK PLAN VALIDATION for UPDATE
        if (dto.DefaultFallbackPlanId.HasValue)
        {
            // Cannot reference itself
            if (dto.DefaultFallbackPlanId.Value == decryptedPlanId)
                throw new BadRequestException(_localizer["Plan.CannotFallbackToSelf"]);
            
            var fallbackPlan = await _planRepo.GetByIdAsync(dto.DefaultFallbackPlanId.Value, null);
            if (fallbackPlan == null)
                throw new NotFoundException(_localizer["Plan.FallbackNotFound"]);
            
            if (!fallbackPlan.IsFreeTier)
                throw new BadRequestException(_localizer["Plan.FallbackMustBeFreeTier"]);
        }

        // ⭐ LIFETIME PLAN VALIDATION for UPDATE
        var targetDurationType = dto.DurationType ?? plan.DurationType;
        
        if (targetDurationType == Domain.Enums.PlanDurationType.Lifetime)
        {
            // Reject invalid values for Lifetime plans
            if (dto.AllowTrial == true)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotHaveTrial"]);
            
            if (dto.AutoRenew == true)
                throw new BadRequestException(_localizer["Plan.LifetimeCannotAutoRenew"]);
            
            // Lifetime plans can only use FullReplace upgrade policy
            if (dto.UpgradePolicy.HasValue && dto.UpgradePolicy.Value != Domain.Enums.UpgradePolicy.FullReplace)
                throw new BadRequestException(_localizer["Plan.LifetimeMustUseFullReplace"]);
            
            // Force grace period to 0
            dto.GracePeriodDays = 0;
        }
        
        if (dto.AllowTrial == true && (!dto.TrialDurationDays.HasValue || dto.TrialDurationDays.Value <= 0))
            throw new BadRequestException(_localizer["Plan.TrialDurationRequired"]);

        // ⭐ MODULE CONFLICT VALIDATION - Check for modules already in projects
        // Skip if called from UpdateWithConfirmationAsync (already validated and filtered)
        if (!skipModuleValidation && dto.ProjectIds != null && dto.ModuleIds != null && dto.ProjectIds.Any() && dto.ModuleIds.Any())
        {
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            var validation = await ValidatePlanModulesAsync(new ValidatePlanModulesRequest
            {
                PlanId = decryptedPlanId,
                ProjectIds = decryptedProjectIds,
                ModuleIds = decryptedModuleIds
            });
            
            if (validation.HasWarnings)
            {
                // Throw exception - frontend must use UpdateWithConfirmationAsync
                throw new PlanModuleConflictException(validation);
            }
        }

        if (dto.Name != null && dto.Name != plan.Name)
        {
            var existing = await _planRepo.GetByNameAsync(dto.Name);
            if (existing != null && existing.Id != decryptedPlanId)
                throw new BadRequestException(_localizer["Plan.NameExists"]);
        }

        _mapper.Map(dto, plan);
        
        // ⭐ FORCE correct values for Free Tier after mapping (defense in depth)
        if (plan.IsFreeTier)
        {
            plan.DurationType = Domain.Enums.PlanDurationType.Lifetime;
            plan.AllowTrial = false;
            plan.TrialDurationDays = null;
            plan.AutoRenew = false;
            plan.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            plan.GracePeriodDays = 0;
            plan.ExportGraceDays = 0;
            plan.DefaultFallbackPlanId = null;
            plan.FallbackAccessMode = Domain.Enums.SubscriptionAccessMode.Full;
        }
        // ⭐ FORCE correct values for Lifetime after mapping (defense in depth)
        else if (plan.DurationType == Domain.Enums.PlanDurationType.Lifetime)
        {
            plan.AllowTrial = false;
            plan.AutoRenew = false;
            plan.UpgradePolicy = Domain.Enums.UpgradePolicy.FullReplace;
            plan.GracePeriodDays = 0;
        }

        // Update prices if provided
        if (dto.Prices != null)
        {
            plan.PlanPrices.Clear();
            foreach (var priceDto in dto.Prices)
            {
                plan.PlanPrices.Add(new PlanPrice
                {
                    PlanId = plan.Id,
                    Currency = priceDto.Currency,
                    Amount = priceDto.Amount
                });
            }
        }

        // Update projects if provided - decrypt ProjectIds using mapper
        if (dto.ProjectIds != null)
        {
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            
            plan.PlanProjects.Clear();
            foreach (var projectId in decryptedProjectIds)
            {
                var project = await _projectRepo.GetByIdAsync(projectId, null);
                if (project == null)
                    throw new NotFoundException(_localizer["Project.NotFound"]);

                plan.PlanProjects.Add(new PlanProject
                {
                    PlanId = plan.Id,
                    ProjectId = projectId
                });
            }
        }

        // Update modules if provided - decrypt ModuleIds using mapper
        if (dto.ModuleIds != null)
        {
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            plan.PlanModules.Clear();
            foreach (var moduleId in decryptedModuleIds)
            {
                var module = await _moduleRepo.GetByIdAsync(moduleId, null);
                if (module == null)
                    throw new NotFoundException(_localizer["Module.NotFound"]);

                plan.PlanModules.Add(new PlanModule
                {
                    PlanId = plan.Id,
                    ModuleId = moduleId
                });
            }
        }

        await _planRepo.UpdateAsync(plan);
        
        // ⭐ SYNC ENTITLEMENTS - update entitlements to match current projects/modules
        if (dto.ProjectIds != null || dto.ModuleIds != null)
        {
            await SyncEntitlementsForPlanAsync(plan);
        }
        
        await _unitOfWork.SaveChangesAsync();

        var result = await _planRepo.GetWithDetailsAsync(decryptedPlanId);
        return _mapper.Map<PlanDto>(result);
    }

    public async Task<bool> DeleteAsync(PlanIdRequest request)
    {
        // Decrypt Plan ID using AutoMapper (SYNFLOX ID encryption rule compliance)
        var decryptedPlanId = _mapper.Map<Guid>(request);
        
        var plan = await _planRepo.GetByIdAsync(decryptedPlanId, null);
        if (plan == null)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // Check if plan has active subscriptions (BLOCKING)
        var hasActiveSubscriptions = await _subscriptionRepo.HasActiveSubscriptionsForPlanAsync(decryptedPlanId);
        if (hasActiveSubscriptions)
            throw new InvalidOperationException(_localizer["Plan.HasActiveSubscriptions"]);

        // ⭐ CASCADE 1: Nullify child plan references (they become root plans)
        await _planRepo.NullifyChildPlanReferencesAsync(decryptedPlanId);

        // ⭐ CASCADE 2: Nullify fallback plan references
        await _planRepo.NullifyFallbackReferencesAsync(decryptedPlanId);

        // ⭐ CASCADE 3: Soft delete all entitlements
        await _entitlementRepo.DeleteAllByPlanIdAsync(decryptedPlanId);
        
        // ⭐ CASCADE 4: Soft delete the plan itself
        await _planRepo.DeleteAsync(decryptedPlanId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    
    public async Task<IEnumerable<PlanDto>> GetFreeTierPlansAsync()
    {
        var freeTierPlans = await _planRepo.GetFreeTierPlansAsync();
        return _mapper.Map<IEnumerable<PlanDto>>(freeTierPlans);
    }
    
    #region Plan Hierarchy Helpers
    
    /// <summary>
    /// Recursively collect all inherited project and module IDs from the parent chain
    /// </summary>
    private async Task CollectInheritedFeaturesAsync(
        SubscriptionPlan plan, 
        HashSet<Guid> projectIds, 
        HashSet<Guid> moduleIds)
    {
        // Add this plan's projects
        foreach (var pp in plan.PlanProjects)
        {
            projectIds.Add(pp.ProjectId);
        }
        
        // Add this plan's modules
        foreach (var pm in plan.PlanModules)
        {
            moduleIds.Add(pm.ModuleId);
        }
        
        // Recursively collect from parent
        if (plan.ParentPlanId.HasValue)
        {
            var parentPlan = await _planRepo.GetWithDetailsAsync(plan.ParentPlanId.Value);
            if (parentPlan != null)
            {
                await CollectInheritedFeaturesAsync(parentPlan, projectIds, moduleIds);
            }
        }
    }
    
    /// <summary>
    /// Get all plans that can be set as parent (no circular reference)
    /// </summary>
    public async Task<IEnumerable<PlanDto>> GetAvailableParentPlansAsync(Guid? excludePlanId = null)
    {
        var allPlans = await _planRepo.GetAllAsync();
        
        if (!excludePlanId.HasValue)
        {
            return _mapper.Map<IEnumerable<PlanDto>>(allPlans);
        }
        
        // Exclude the plan itself and all its descendants
        var descendantIds = new HashSet<Guid>();
        await CollectDescendantIdsAsync(excludePlanId.Value, descendantIds, allPlans.ToList());
        descendantIds.Add(excludePlanId.Value); // Also exclude self
        
        var availablePlans = allPlans.Where(p => !descendantIds.Contains(p.Id));
        return _mapper.Map<IEnumerable<PlanDto>>(availablePlans);
    }
    
    /// <summary>
    /// Recursively collect all descendant plan IDs
    /// </summary>
    private async Task CollectDescendantIdsAsync(
        Guid planId, 
        HashSet<Guid> descendantIds, 
        List<SubscriptionPlan> allPlans)
    {
        var children = allPlans.Where(p => p.ParentPlanId == planId).ToList();
        foreach (var child in children)
        {
            descendantIds.Add(child.Id);
            await CollectDescendantIdsAsync(child.Id, descendantIds, allPlans);
        }
    }
    
    #endregion
    
    #region Module Conflict Validation
    
    /// <summary>
    /// Validates if any standalone modules are already included in projects
    /// Returns warnings for conflicts, not errors - user can confirm to proceed
    /// </summary>
    public async Task<PlanValidationResultDto> ValidatePlanModulesAsync(ValidatePlanModulesRequest request)
    {
        var result = new PlanValidationResultDto { IsValid = true };
        
        if (!request.ProjectIds.Any() || !request.ModuleIds.Any())
        {
            // No conflicts possible if either list is empty
            result.ValidStandaloneModuleIds = request.ModuleIds;
            return result;
        }
        
        // Get all modules that belong to the selected projects
        var moduleIdsInProjects = new Dictionary<Guid, (Guid ProjectId, string ProjectName)>();
        
        foreach (var projectId in request.ProjectIds)
        {
            var project = await _projectRepo.GetByIdAsync(projectId, new[] { "ProjectModules.Module" });
            if (project == null) continue;
            
            foreach (var pm in project.ProjectModules)
            {
                if (!moduleIdsInProjects.ContainsKey(pm.ModuleId))
                {
                    moduleIdsInProjects[pm.ModuleId] = (projectId, project.Name);
                }
            }
        }
        
        // Check each requested standalone module for conflicts
        foreach (var moduleId in request.ModuleIds)
        {
            if (moduleIdsInProjects.TryGetValue(moduleId, out var projectInfo))
            {
                // This module is already in one of the selected projects
                var module = await _moduleRepo.GetByIdAsync(moduleId, null);
                
                result.ModuleConflicts.Add(new PlanModuleConflictDto
                {
                    ModuleId = moduleId,
                    ModuleName = module?.Name ?? "Unknown Module",
                    ProjectId = projectInfo.ProjectId,
                    ProjectName = projectInfo.ProjectName
                });
            }
            else
            {
                // This module is truly standalone
                result.ValidStandaloneModuleIds.Add(moduleId);
            }
        }
        
        // Build warning message if there are conflicts
        if (result.ModuleConflicts.Any())
        {
            var conflictCount = result.ModuleConflicts.Count;
            var moduleNames = string.Join(", ", result.ModuleConflicts.Select(c => c.ModuleName));
            
            result.WarningMessage = string.Format(_localizer["Plan.ModuleConflictWarning"], conflictCount, moduleNames);
        }
        
        return result;
    }
    
    /// <summary>
    /// Create plan with explicit confirmation to remove duplicate modules
    /// </summary>
    public async Task<PlanDto> CreateWithConfirmationAsync(CreatePlanWithConfirmationDto dto)
    {
        // If ModuleIds and ProjectIds provided, validate for conflicts
        if (dto.ProjectIds.Any() && dto.ModuleIds.Any())
        {
            // Decrypt IDs
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            // Build encrypted→decrypted mapping to preserve encrypted IDs
            var encryptedToDecrypted = new Dictionary<Guid, Guid>();
            for (int i = 0; i < dto.ModuleIds.Count; i++)
            {
                encryptedToDecrypted[dto.ModuleIds[i]] = decryptedModuleIds[i];
            }
            
            // Validate
            var validation = await ValidatePlanModulesAsync(new ValidatePlanModulesRequest
            {
                PlanId = null, // New plan
                ProjectIds = decryptedProjectIds,
                ModuleIds = decryptedModuleIds
            });
            
            if (validation.HasWarnings && !dto.ConfirmRemoveDuplicates)
            {
                // Return the validation result as an exception with details
                throw new PlanModuleConflictException(validation);
            }
            
            // If confirmed, filter to only valid standalone modules (keep encrypted IDs)
            if (validation.HasWarnings && dto.ConfirmRemoveDuplicates)
            {
                var validDecryptedSet = validation.ValidStandaloneModuleIds.ToHashSet();
                
                // Keep only encrypted IDs whose decrypted values are in the valid set
                dto.ModuleIds = encryptedToDecrypted
                    .Where(kvp => validDecryptedSet.Contains(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
        
        // Proceed with normal create - skip validation since we already handled it
        return await CreateAsyncInternal(dto, skipModuleValidation: true);
    }
    
    /// <summary>
    /// Update plan with explicit confirmation to remove duplicate modules
    /// </summary>
    public async Task<PlanDto?> UpdateWithConfirmationAsync(PlanIdRequest request, UpdatePlanWithConfirmationDto dto)
    {
        // If ModuleIds and ProjectIds provided, validate for conflicts
        if (dto.ModuleIds != null && dto.ProjectIds != null && dto.ModuleIds.Any() && dto.ProjectIds.Any())
        {
            // Decrypt IDs
            var decryptedProjectIds = _mapper.Map<IEnumerable<Guid>>(new ProjectIdsRequest { ProjectIds = dto.ProjectIds }).ToList();
            var decryptedModuleIds = _mapper.Map<IEnumerable<Guid>>(new ModuleIdsRequest { ModuleIds = dto.ModuleIds }).ToList();
            
            // Build encrypted→decrypted mapping to preserve encrypted IDs
            var encryptedToDecrypted = new Dictionary<Guid, Guid>();
            for (int i = 0; i < dto.ModuleIds.Count; i++)
            {
                encryptedToDecrypted[dto.ModuleIds[i]] = decryptedModuleIds[i];
            }
            
            // Validate
            var validation = await ValidatePlanModulesAsync(new ValidatePlanModulesRequest
            {
                PlanId = _mapper.Map<Guid>(request),
                ProjectIds = decryptedProjectIds,
                ModuleIds = decryptedModuleIds
            });
            
            if (validation.HasWarnings && !dto.ConfirmRemoveDuplicates)
            {
                // Return the validation result as an exception with details
                throw new PlanModuleConflictException(validation);
            }
            
            // If confirmed, filter to only valid standalone modules (keep encrypted IDs)
            if (validation.HasWarnings && dto.ConfirmRemoveDuplicates)
            {
                var validDecryptedSet = validation.ValidStandaloneModuleIds.ToHashSet();
                
                // Keep only encrypted IDs whose decrypted values are in the valid set
                dto.ModuleIds = encryptedToDecrypted
                    .Where(kvp => validDecryptedSet.Contains(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
        
        // Proceed with normal update - skip validation since we already handled it
        return await UpdateAsyncInternal(request, dto, skipModuleValidation: true);
    }
    
    #endregion
    
    #region Entitlement Auto-Sync Helpers
    
    /// <summary>
    /// Create entitlements with Full Access for all projects/modules in the plan.
    /// For each PROJECT: creates project entitlement + entitlement for each module in that project.
    /// For each standalone MODULE: creates module entitlement (no parent).
    /// </summary>
    private async Task CreateEntitlementsForPlanAsync(SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        
        // Collect all module IDs that belong to included projects
        var moduleIdsInProjects = new HashSet<Guid>();
        
        // Create entitlements for all projects AND their modules
        foreach (var planProject in plan.PlanProjects)
        {
            // Get the project with its modules
            var project = await _projectRepo.GetByIdAsync(planProject.ProjectId, new[] { "ProjectModules.Module" });
            if (project == null) continue;
            
            // Create project entitlement
            var projectEntitlement = CreateFullAccessEntitlement(plan.Id, planProject.ProjectId, null, null, now);
            await _entitlementRepo.AddAsync(projectEntitlement);
            
            // Create entitlements for all modules in this project
            foreach (var pm in project.ProjectModules)
            {
                moduleIdsInProjects.Add(pm.ModuleId);
                
                var moduleEntitlement = CreateFullAccessEntitlement(
                    plan.Id, 
                    null, 
                    pm.ModuleId, 
                    planProject.ProjectId, // ParentProjectId
                    now);
                await _entitlementRepo.AddAsync(moduleEntitlement);
            }
        }
        
        // Create entitlements for standalone modules (not in any included project)
        foreach (var planModule in plan.PlanModules)
        {
            // Skip if this module is already included via a project
            if (moduleIdsInProjects.Contains(planModule.ModuleId))
                continue;
            
            var moduleEntitlement = CreateFullAccessEntitlement(
                plan.Id, 
                null, 
                planModule.ModuleId, 
                null, // No parent - standalone
                now);
            await _entitlementRepo.AddAsync(moduleEntitlement);
        }
        
        // Increment entitlement version
        plan.EntitlementVersion++;
    }
    
    /// <summary>
    /// Sync entitlements to match current projects/modules in the plan.
    /// Handles hierarchical permissions properly.
    /// </summary>
    private async Task SyncEntitlementsForPlanAsync(SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        var existingEntitlements = (await _entitlementRepo.GetByPlanIdAsync(plan.Id)).ToList();
        
        // Get current project/module IDs in the plan
        var currentProjectIds = plan.PlanProjects.Select(pp => pp.ProjectId).ToHashSet();
        var currentStandaloneModuleIds = plan.PlanModules.Select(pm => pm.ModuleId).ToHashSet();
        
        // Collect all module IDs that belong to included projects
        var moduleIdsInProjects = new HashSet<Guid>();
        var projectModulesMap = new Dictionary<Guid, List<Guid>>(); // ProjectId -> List of ModuleIds
        
        foreach (var projectId in currentProjectIds)
        {
            var project = await _projectRepo.GetByIdAsync(projectId, new[] { "ProjectModules.Module" });
            if (project != null)
            {
                var moduleIds = project.ProjectModules.Select(pm => pm.ModuleId).ToList();
                projectModulesMap[projectId] = moduleIds;
                foreach (var moduleId in moduleIds)
                {
                    moduleIdsInProjects.Add(moduleId);
                }
            }
        }
        
        // Remove standalone modules that are now included in a project
        // (they will be recreated under the project)
        var standaloneToConvert = existingEntitlements
            .Where(e => e.IsStandaloneModule && moduleIdsInProjects.Contains(e.ModuleId!.Value))
            .ToList();
        
        foreach (var entitlement in standaloneToConvert)
        {
            entitlement.IsDeleted = true;
            entitlement.IsActive = false;
            entitlement.DeletedTimestamp = now;
            entitlement.UpdatedTimestamp = now;
            await _entitlementRepo.UpdateAsync(entitlement);
        }
        
        // Find entitlements to remove (project/module no longer in plan)
        var entitlementsToRemove = existingEntitlements
            .Where(e => 
                (e.ProjectId.HasValue && !currentProjectIds.Contains(e.ProjectId.Value)) ||
                (e.IsStandaloneModule && !currentStandaloneModuleIds.Contains(e.ModuleId!.Value) && !moduleIdsInProjects.Contains(e.ModuleId!.Value)) ||
                (e.IsModuleUnderProject && e.ParentProjectId.HasValue && !currentProjectIds.Contains(e.ParentProjectId.Value)))
            .ToList();
        
        // Soft delete removed entitlements
        foreach (var entitlement in entitlementsToRemove)
        {
            entitlement.IsDeleted = true;
            entitlement.IsActive = false;
            entitlement.DeletedTimestamp = now;
            entitlement.UpdatedTimestamp = now;
            await _entitlementRepo.UpdateAsync(entitlement);
        }
        
        // Find existing project IDs with entitlements
        var existingProjectIds = existingEntitlements
            .Where(e => e.ProjectId.HasValue && !e.IsDeleted)
            .Select(e => e.ProjectId!.Value)
            .ToHashSet();
        
        // Add new project entitlements and their module entitlements
        var newProjectIds = currentProjectIds.Except(existingProjectIds);
        
        foreach (var projectId in newProjectIds)
        {
            // Create project entitlement
            var projectEntitlement = CreateFullAccessEntitlement(plan.Id, projectId, null, null, now);
            await _entitlementRepo.AddAsync(projectEntitlement);
            
            // Create module entitlements for this project
            if (projectModulesMap.TryGetValue(projectId, out var moduleIds))
            {
                foreach (var moduleId in moduleIds)
                {
                    var moduleEntitlement = CreateFullAccessEntitlement(plan.Id, null, moduleId, projectId, now);
                    await _entitlementRepo.AddAsync(moduleEntitlement);
                }
            }
        }
        
        // For existing projects, check if any new modules were added to the project
        foreach (var projectId in currentProjectIds.Intersect(existingProjectIds))
        {
            if (!projectModulesMap.TryGetValue(projectId, out var moduleIds))
                continue;
            
            // Get project entitlement to inherit its access level
            var projectEntitlement = existingEntitlements
                .FirstOrDefault(e => e.ProjectId == projectId && !e.IsDeleted);
            
            var existingModuleIds = existingEntitlements
                .Where(e => e.IsModuleUnderProject && e.ParentProjectId == projectId && !e.IsDeleted)
                .Select(e => e.ModuleId!.Value)
                .ToHashSet();
            
            var newModuleIds = moduleIds.Except(existingModuleIds);
            
            foreach (var moduleId in newModuleIds)
            {
                // Inherit from parent project's access level
                var moduleEntitlement = projectEntitlement != null
                    ? CreateInheritedEntitlement(plan.Id, moduleId, projectId, projectEntitlement, now)
                    : CreateFullAccessEntitlement(plan.Id, null, moduleId, projectId, now);
                    
                await _entitlementRepo.AddAsync(moduleEntitlement);
            }
        }
        
        // Add new standalone module entitlements
        var existingStandaloneModuleIds = existingEntitlements
            .Where(e => e.IsStandaloneModule && !e.IsDeleted)
            .Select(e => e.ModuleId!.Value)
            .ToHashSet();
        
        var newStandaloneModuleIds = currentStandaloneModuleIds
            .Except(existingStandaloneModuleIds)
            .Except(moduleIdsInProjects); // Don't add as standalone if in a project
        
        foreach (var moduleId in newStandaloneModuleIds)
        {
            var moduleEntitlement = CreateFullAccessEntitlement(plan.Id, null, moduleId, null, now);
            await _entitlementRepo.AddAsync(moduleEntitlement);
        }
        
        // Increment entitlement version if any changes were made
        plan.EntitlementVersion++;
    }
    
    /// <summary>
    /// Creates a PlanEntitlement with Full Access
    /// </summary>
    private PlanEntitlement CreateFullAccessEntitlement(
        Guid planId, 
        Guid? projectId, 
        Guid? moduleId, 
        Guid? parentProjectId, 
        DateTime now)
    {
        return new PlanEntitlement
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            ProjectId = projectId,
            ModuleId = moduleId,
            ParentProjectId = parentProjectId,
            IsOverride = false,
            AccessLevel = Domain.Enums.EntitlementAccessLevel.Full,
            CanCreate = true,
            CanRead = true,
            CanUpdate = true,
            CanDelete = true,
            CanExport = true,
            DisplayInMenu = true,
            IsActive = true,
            IsDeleted = false,
            CreatedTimestamp = now
        };
    }
    
    /// <summary>
    /// Creates a PlanEntitlement that inherits from parent project's settings
    /// </summary>
    private PlanEntitlement CreateInheritedEntitlement(
        Guid planId,
        Guid moduleId,
        Guid parentProjectId,
        PlanEntitlement parentEntitlement,
        DateTime now)
    {
        return new PlanEntitlement
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            ProjectId = null,
            ModuleId = moduleId,
            ParentProjectId = parentProjectId,
            IsOverride = false, // Inherits from parent
            AccessLevel = parentEntitlement.AccessLevel,
            CanCreate = parentEntitlement.CanCreate,
            CanRead = parentEntitlement.CanRead,
            CanUpdate = parentEntitlement.CanUpdate,
            CanDelete = parentEntitlement.CanDelete,
            CanExport = parentEntitlement.CanExport,
            DisplayInMenu = parentEntitlement.DisplayInMenu,
            IsActive = true,
            IsDeleted = false,
            CreatedTimestamp = now
        };
    }
    
    #endregion
}
