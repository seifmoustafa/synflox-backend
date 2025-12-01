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
            new[] { "PlanPrices", "PlanProjects", "PlanModules", "ParentPlan", "DefaultFallbackPlan" },
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

        // Check if plan has active subscriptions
        var hasActiveSubscriptions = await _subscriptionRepo.HasActiveSubscriptionsForPlanAsync(decryptedPlanId);
        if (hasActiveSubscriptions)
            throw new InvalidOperationException(_localizer["Plan.HasActiveSubscriptions"]);

        // ⭐ SOFT DELETE ENTITLEMENTS when plan is deleted
        await _entitlementRepo.DeleteAllByPlanIdAsync(decryptedPlanId);
        
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
    
    #region Entitlement Auto-Sync Helpers
    
    /// <summary>
    /// Create entitlements with Full Access for all projects/modules in the plan
    /// Called when a new plan is created
    /// </summary>
    private async Task CreateEntitlementsForPlanAsync(SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        
        // Create entitlements for all projects
        foreach (var planProject in plan.PlanProjects)
        {
            var entitlement = new PlanEntitlement
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                ProjectId = planProject.ProjectId,
                ModuleId = null,
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
            await _entitlementRepo.AddAsync(entitlement);
        }
        
        // Create entitlements for all modules
        foreach (var planModule in plan.PlanModules)
        {
            var entitlement = new PlanEntitlement
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                ProjectId = null,
                ModuleId = planModule.ModuleId,
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
            await _entitlementRepo.AddAsync(entitlement);
        }
        
        // Increment entitlement version
        plan.EntitlementVersion++;
    }
    
    /// <summary>
    /// Sync entitlements to match current projects/modules in the plan
    /// - Removes entitlements for projects/modules no longer in the plan
    /// - Adds entitlements for new projects/modules with Full Access
    /// - Preserves existing entitlements and their customizations
    /// </summary>
    private async Task SyncEntitlementsForPlanAsync(SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        var existingEntitlements = (await _entitlementRepo.GetByPlanIdAsync(plan.Id)).ToList();
        
        // Get current project/module IDs in the plan
        var currentProjectIds = plan.PlanProjects.Select(pp => pp.ProjectId).ToHashSet();
        var currentModuleIds = plan.PlanModules.Select(pm => pm.ModuleId).ToHashSet();
        
        // Find entitlements to remove (project/module no longer in plan)
        var entitlementsToRemove = existingEntitlements
            .Where(e => 
                (e.ProjectId.HasValue && !currentProjectIds.Contains(e.ProjectId.Value)) ||
                (e.ModuleId.HasValue && !currentModuleIds.Contains(e.ModuleId.Value)))
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
        
        // Find new projects that need entitlements
        var existingProjectIds = existingEntitlements
            .Where(e => e.ProjectId.HasValue)
            .Select(e => e.ProjectId!.Value)
            .ToHashSet();
        
        var newProjectIds = currentProjectIds.Except(existingProjectIds);
        
        foreach (var projectId in newProjectIds)
        {
            var entitlement = new PlanEntitlement
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                ProjectId = projectId,
                ModuleId = null,
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
            await _entitlementRepo.AddAsync(entitlement);
        }
        
        // Find new modules that need entitlements
        var existingModuleIds = existingEntitlements
            .Where(e => e.ModuleId.HasValue)
            .Select(e => e.ModuleId!.Value)
            .ToHashSet();
        
        var newModuleIds = currentModuleIds.Except(existingModuleIds);
        
        foreach (var moduleId in newModuleIds)
        {
            var entitlement = new PlanEntitlement
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                ProjectId = null,
                ModuleId = moduleId,
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
            await _entitlementRepo.AddAsync(entitlement);
        }
        
        // Increment entitlement version if any changes were made
        if (entitlementsToRemove.Any() || newProjectIds.Any() || newModuleIds.Any())
        {
            plan.EntitlementVersion++;
        }
    }
    
    #endregion
}
