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
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository planRepo,
        IProjectRepository projectRepo,
        IModuleRepository moduleRepo,
        ISubscriptionRepository subscriptionRepo,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _planRepo = planRepo;
        _projectRepo = projectRepo;
        _moduleRepo = moduleRepo;
        _subscriptionRepo = subscriptionRepo;
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

        // Must include at least one project or module
        if (!dto.ProjectIds.Any() && !dto.ModuleIds.Any())
            throw new BadRequestException(_localizer["Plan.MustIncludeContent"]);

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
            new[] { "PlanPrices" },
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

        await _planRepo.DeleteAsync(decryptedPlanId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    
    public async Task<IEnumerable<PlanDto>> GetFreeTierPlansAsync()
    {
        var freeTierPlans = await _planRepo.GetFreeTierPlansAsync();
        return _mapper.Map<IEnumerable<PlanDto>>(freeTierPlans);
    }
}
