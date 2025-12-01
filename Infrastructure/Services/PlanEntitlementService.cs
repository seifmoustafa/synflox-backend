using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.PlanEntitlements;
using Application.Services;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Application.Services_Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service implementation for plan-level entitlement operations
/// </summary>
public class PlanEntitlementService : IPlanEntitlementService
{
    private readonly IPlanEntitlementRepository _entitlementRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IModuleRepository _moduleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;

    public PlanEntitlementService(
        IPlanEntitlementRepository entitlementRepo,
        ISubscriptionPlanRepository planRepo,
        IProjectRepository projectRepo,
        IModuleRepository moduleRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILocalizationService localizer)
    {
        _entitlementRepo = entitlementRepo;
        _planRepo = planRepo;
        _projectRepo = projectRepo;
        _moduleRepo = moduleRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlanEntitlementDto>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var entitlements = await _entitlementRepo.GetByPlanIdWithDetailsAsync(planId, cancellationToken);
        return _mapper.Map<IEnumerable<PlanEntitlementDto>>(entitlements);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entitlement = await _entitlementRepo.GetByIdAsync(id, new[] { "Plan", "Project", "Module" });
        if (entitlement == null || entitlement.IsDeleted)
            return null;
        
        return _mapper.Map<PlanEntitlementDto>(entitlement);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlementDto> CreateAsync(CreatePlanEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        // Validate target (must have exactly one of ProjectId or ModuleId)
        if (!request.ProjectId.HasValue && !request.ModuleId.HasValue)
            throw new BadRequestException(_localizer["PlanEntitlement.MustHaveTarget"]);
        
        if (request.ProjectId.HasValue && request.ModuleId.HasValue)
            throw new BadRequestException(_localizer["PlanEntitlement.OnlyOneTarget"]);
        
        // Validate plan exists
        var plan = await _planRepo.GetByIdAsync(request.PlanId, null);
        if (plan == null || plan.IsDeleted)
            throw new NotFoundException(_localizer["Plan.NotFound"]);
        
        // Validate target exists
        if (request.ProjectId.HasValue)
        {
            var project = await _projectRepo.GetByIdAsync(request.ProjectId.Value, null);
            if (project == null || project.IsDeleted)
                throw new NotFoundException(_localizer["Project.NotFound"]);
        }
        
        if (request.ModuleId.HasValue)
        {
            var module = await _moduleRepo.GetByIdAsync(request.ModuleId.Value, null);
            if (module == null || module.IsDeleted)
                throw new NotFoundException(_localizer["Module.NotFound"]);
        }
        
        // Check for duplicate
        var exists = await _entitlementRepo.ExistsAsync(request.PlanId, request.ProjectId, request.ModuleId, cancellationToken);
        if (exists)
            throw new BadRequestException(_localizer["PlanEntitlement.AlreadyExists"]);
        
        // Create entitlement
        var entitlement = new PlanEntitlement
        {
            Id = Guid.NewGuid(),
            PlanId = request.PlanId,
            ProjectId = request.ProjectId,
            ModuleId = request.ModuleId,
            AccessLevel = request.AccessLevel,
            CanCreate = request.CanCreate,
            CanRead = request.CanRead,
            CanUpdate = request.CanUpdate,
            CanDelete = request.CanDelete,
            CanExport = request.CanExport,
            DisplayInMenu = request.DisplayInMenu,
            Features = request.Features,
            IsActive = true,
            IsDeleted = false,
            CreatedTimestamp = DateTime.UtcNow
        };

        await _entitlementRepo.AddAsync(entitlement);
        
        // Increment plan's entitlement version (invalidates client caches)
        plan.EntitlementVersion++;
        await _planRepo.UpdateAsync(plan);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with details
        var created = await _entitlementRepo.GetByIdAsync(entitlement.Id, new[] { "Plan", "Project", "Module" });
        return _mapper.Map<PlanEntitlementDto>(created);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlementDto> UpdateAsync(UpdatePlanEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        var entitlement = await _entitlementRepo.GetByIdAsync(request.Id, new[] { "Plan", "Project", "Module" });
        if (entitlement == null || entitlement.IsDeleted)
            throw new NotFoundException(_localizer["PlanEntitlement.NotFound"]);
        
        // Update fields if provided
        if (request.AccessLevel.HasValue)
            entitlement.AccessLevel = request.AccessLevel.Value;
        
        if (request.CanCreate.HasValue)
            entitlement.CanCreate = request.CanCreate.Value;
        
        if (request.CanRead.HasValue)
            entitlement.CanRead = request.CanRead.Value;
        
        if (request.CanUpdate.HasValue)
            entitlement.CanUpdate = request.CanUpdate.Value;
        
        if (request.CanDelete.HasValue)
            entitlement.CanDelete = request.CanDelete.Value;
        
        if (request.CanExport.HasValue)
            entitlement.CanExport = request.CanExport.Value;
        
        if (request.DisplayInMenu.HasValue)
            entitlement.DisplayInMenu = request.DisplayInMenu.Value;
        
        if (request.Features != null)
            entitlement.Features = request.Features;
        
        if (request.IsActive.HasValue)
            entitlement.IsActive = request.IsActive.Value;

        entitlement.UpdatedTimestamp = DateTime.UtcNow;

        await _entitlementRepo.UpdateAsync(entitlement);
        
        // Increment plan's entitlement version
        var plan = await _planRepo.GetByIdAsync(entitlement.PlanId, null);
        if (plan != null)
        {
            plan.EntitlementVersion++;
            await _planRepo.UpdateAsync(plan);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PlanEntitlementDto>(entitlement);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entitlement = await _entitlementRepo.GetByIdAsync(id, null);
        if (entitlement == null || entitlement.IsDeleted)
            throw new NotFoundException(_localizer["PlanEntitlement.NotFound"]);
        
        // Soft delete with full audit trail
        entitlement.IsDeleted = true;
        entitlement.IsActive = false;
        entitlement.DeletedTimestamp = DateTime.UtcNow;
        entitlement.UpdatedTimestamp = DateTime.UtcNow;
        
        await _entitlementRepo.UpdateAsync(entitlement);
        
        // Increment plan's entitlement version
        var plan = await _planRepo.GetByIdAsync(entitlement.PlanId, null);
        if (plan != null)
        {
            plan.EntitlementVersion++;
            await _planRepo.UpdateAsync(plan);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAllByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        await _entitlementRepo.DeleteAllByPlanIdAsync(planId, cancellationToken);
        
        // Increment plan's entitlement version
        var plan = await _planRepo.GetByIdAsync(planId, null);
        if (plan != null)
        {
            plan.EntitlementVersion++;
            await _planRepo.UpdateAsync(plan);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CopyEntitlementsAsync(Guid sourcePlanId, Guid targetPlanId, CancellationToken cancellationToken = default)
    {
        // Validate both plans exist
        var sourcePlan = await _planRepo.GetByIdAsync(sourcePlanId, null);
        if (sourcePlan == null || sourcePlan.IsDeleted)
            throw new NotFoundException(_localizer["Plan.NotFound"]);
        
        var targetPlan = await _planRepo.GetByIdAsync(targetPlanId, null);
        if (targetPlan == null || targetPlan.IsDeleted)
            throw new NotFoundException(_localizer["Plan.NotFound"]);

        // First, delete existing entitlements on target plan to avoid duplicates
        await _entitlementRepo.DeleteAllByPlanIdAsync(targetPlanId, cancellationToken);
        
        // Then copy from source
        await _entitlementRepo.CopyEntitlementsAsync(sourcePlanId, targetPlanId, cancellationToken);
        
        // Increment target plan's entitlement version
        targetPlan.EntitlementVersion++;
        await _planRepo.UpdateAsync(targetPlan);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _entitlementRepo.GetCountByPlanIdAsync(planId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlementDto> GrantProjectAccessAsync(Guid planId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreatePlanEntitlementRequest
        {
            PlanId = planId,
            ProjectId = projectId,
            AccessLevel = EntitlementAccessLevel.Full,
            CanCreate = true,
            CanRead = true,
            CanUpdate = true,
            CanDelete = true,
            CanExport = true,
            DisplayInMenu = true
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanEntitlementDto> GrantModuleAccessAsync(Guid planId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreatePlanEntitlementRequest
        {
            PlanId = planId,
            ModuleId = moduleId,
            AccessLevel = EntitlementAccessLevel.Full,
            CanCreate = true,
            CanRead = true,
            CanUpdate = true,
            CanDelete = true,
            CanExport = true,
            DisplayInMenu = true
        }, cancellationToken);
    }
}
