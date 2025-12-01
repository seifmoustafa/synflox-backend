using AutoMapper;
using Domain.Entities.Subscriptions;
using Application.DTOs.Subscriptions;
using Domain.Helpers;
using ModuleDtos = Application.DTOs.ModuleDto;
using ProjectDtos = Application.DTOs.ProjectDto;
using PlanDtos = Application.DTOs.PlanDto;

namespace Application.Mapping;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        // ========== Project Mappings ==========
        CreateMap<Project, ProjectDtos.ProjectDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.ProjectModules.Select(pm => pm.Module)));

        CreateMap<ProjectDtos.CreateProjectDto, Project>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ProjectModules, opt => opt.Ignore());

        CreateMap<ProjectDtos.UpdateProjectDto, Project>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        
        // Decrypt ModuleIds collection using universal converter - returns IEnumerable, will be converted to List
        CreateMap<ModuleDtos.ModuleIdsRequest, IEnumerable<Guid>>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // Decrypt ProjectIds collection using universal converter - returns IEnumerable, will be converted to List
        CreateMap<ProjectDtos.ProjectIdsRequest, IEnumerable<Guid>>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // Decrypt single PlanId for operations requiring encrypted Plan ID
        CreateMap<PlanDtos.PlanIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // Decrypt single ProjectId for operations requiring encrypted Project ID
        CreateMap<ProjectDtos.ProjectIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // Decrypt single ModuleId for operations requiring encrypted Module ID
        CreateMap<ModuleDtos.ModuleIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // ========== Module Mappings ==========
        CreateMap<Module, ModuleDtos.ModuleDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive));
            // Removed Projects mapping - modules don't own the relationship!

        CreateMap<ModuleDtos.CreateModuleDto, Module>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ProjectModules, opt => opt.Ignore())
            .ForMember(d => d.PlanModules, opt => opt.Ignore());

        CreateMap<ModuleDtos.UpdateModuleDto, Module>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ========== Plan Price Mappings ==========
        CreateMap<PlanPrice, PlanDtos.PlanPriceDto>();
        CreateMap<PlanDtos.PlanPriceDto, PlanPrice>();

        // ========== Subscription Plan Mappings ==========
        CreateMap<SubscriptionPlan, PlanDtos.PlanDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Prices, opt => opt.MapFrom(s => s.PlanPrices))
            .ForMember(d => d.DurationType, opt => opt.MapFrom(s => s.DurationType))
            .ForMember(d => d.IsLifetimePlan, opt => opt.MapFrom(s => s.IsLifetimePlan))
            .ForMember(d => d.DurationDescription, opt => opt.MapFrom(s => PlanDurationHelper.GetDurationDescription(s.DurationType)))
            // Free Tier & Fallback
            .ForMember(d => d.IsFreeTier, opt => opt.MapFrom(s => s.IsFreeTier))
            .ForMember(d => d.FallbackAccessMode, opt => opt.MapFrom(s => s.FallbackAccessMode))
            .ForMember(d => d.ExportGraceDays, opt => opt.MapFrom(s => s.ExportGraceDays))
            .ForMember(d => d.DefaultFallbackPlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.DefaultFallbackPlanId))
            .ForMember(d => d.DefaultFallbackPlanName, opt => opt.MapFrom(s => s.DefaultFallbackPlan != null ? s.DefaultFallbackPlan.Name : null))
            .ForMember(d => d.ShowLockedModulesInMenu, opt => opt.MapFrom(s => s.ShowLockedModulesInMenu))
            .ForMember(d => d.LockedItemStyle, opt => opt.MapFrom(s => s.LockedItemStyle))
            // Project & Module counts
            .ForMember(d => d.ProjectCount, opt => opt.MapFrom(s => s.PlanProjects.Count))
            .ForMember(d => d.ModuleCount, opt => opt.MapFrom(s => s.PlanModules.Count))
            // Plan Hierarchy
            .ForMember(d => d.ParentPlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ParentPlanId))
            .ForMember(d => d.ParentPlanName, opt => opt.MapFrom(s => s.ParentPlan != null ? s.ParentPlan.Name : null))
            .ForMember(d => d.DisplayOrder, opt => opt.MapFrom(s => s.DisplayOrder))
            .ForMember(d => d.ChildPlanCount, opt => opt.MapFrom(s => s.ChildPlans.Count))
            .ForMember(d => d.InheritedProjectsCount, opt => opt.Ignore()) // Calculated in service
            .ForMember(d => d.InheritedModulesCount, opt => opt.Ignore()); // Calculated in service

        CreateMap<SubscriptionPlan, PlanDtos.PlanDetailsDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Prices, opt => opt.MapFrom(s => s.PlanPrices))
            .ForMember(d => d.DurationType, opt => opt.MapFrom(s => s.DurationType))
            .ForMember(d => d.IsLifetimePlan, opt => opt.MapFrom(s => s.IsLifetimePlan))
            .ForMember(d => d.DurationDescription, opt => opt.MapFrom(s => PlanDurationHelper.GetDurationDescription(s.DurationType)))
            .ForMember(d => d.Projects, opt => opt.MapFrom(s => 
                s.PlanProjects.Select(pp => pp.Project)))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.PlanModules.Select(pm => pm.Module)));

        CreateMap<CreateSubscriptionPlanDto, SubscriptionPlan>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.DurationType, opt => opt.MapFrom(s => s.DurationType))
            .ForMember(d => d.PlanPrices, opt => opt.Ignore())
            .ForMember(d => d.PlanProjects, opt => opt.Ignore())
            .ForMember(d => d.PlanModules, opt => opt.Ignore())
            .ForMember(d => d.Subscriptions, opt => opt.Ignore())
            // Entitlement fields - decrypt DefaultFallbackPlanId
            .ForMember(d => d.DefaultFallbackPlanId, opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid?>(s => s.DefaultFallbackPlanId))
            .ForMember(d => d.DefaultFallbackPlan, opt => opt.Ignore());

        CreateMap<PlanDtos.UpdatePlanDto, SubscriptionPlan>()
            .ForMember(d => d.DefaultFallbackPlanId, opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid?>(s => s.DefaultFallbackPlanId))
            .ForMember(d => d.DefaultFallbackPlan, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ========== Subscription Plan Feature Mappings ==========
        CreateMap<Project, SubscriptionProjectDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.ProjectModules.Select(pm => pm.Module)));

        CreateMap<Module, SubscriptionModuleDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));

        // ========== Subscription Mappings ==========
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Company.Name))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan.Name))
            .ForMember(d => d.PlanDescription, opt => opt.MapFrom(s => s.Plan.Description))
            .ForMember(d => d.IsLifetime, opt => opt.MapFrom(s => s.IsLifetime))
            // Next subscription for deferred upgrades
            .ForMember(d => d.NextSubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.NextSubscriptionId))
            .ForMember(d => d.NextSubscriptionPlanName, opt => opt.MapFrom(s => s.NextSubscription != null ? s.NextSubscription.Plan.Name : null))
            .ForMember(d => d.NextSubscriptionActivationDateUtc, opt => opt.MapFrom(s => s.NextSubscriptionActivationDateUtc))
            // Parent subscription for upgrade/renewal chain
            .ForMember(d => d.ParentSubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ParentSubscriptionId))
            .ForMember(d => d.ParentSubscriptionDisplayName, opt => opt.MapFrom(s => 
                s.ParentSubscription != null ? $"{s.ParentSubscription.Plan.Name} ({s.ParentSubscription.StartDateUtc:yyyy-MM-dd})" : null))
            .ForMember(d => d.OfflineLicenseKey, opt => opt.Ignore()) // Set manually based on user role
            .ForMember(d => d.LicenseKeyGeneratedAt, opt => opt.MapFrom(s => s.LicenseKeyGeneratedAt))
            .ForMember(d => d.LicenseKeyVersion, opt => opt.MapFrom(s => s.LicenseKeyVersion))
            // Plan Features
            .ForMember(d => d.Projects, opt => opt.MapFrom(s => 
                s.Plan.PlanProjects.Select(pp => pp.Project)))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.Plan.PlanModules.Select(pm => pm.Module)))
            .ForMember(d => d.CustomFeatures, opt => opt.MapFrom(s => s.Plan.CustomFeatures))
            // Access Control (Enterprise Entitlement System)
            .ForMember(d => d.AccessMode, opt => opt.MapFrom(s => s.AccessMode))
            // Default fallback from plan (read-only)
            .ForMember(d => d.DefaultFallbackPlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.Plan.DefaultFallbackPlanId))
            .ForMember(d => d.DefaultFallbackPlanName, opt => opt.MapFrom(s => s.Plan.DefaultFallbackPlan != null ? s.Plan.DefaultFallbackPlan.Name : null))
            .ForMember(d => d.ExportDeadlineUtc, opt => opt.MapFrom(s => s.ExportDeadlineUtc))
            .ForMember(d => d.EntitlementsVersion, opt => opt.MapFrom(s => s.EntitlementsVersion))
            .ForMember(d => d.AccessRestrictionMessage, opt => opt.MapFrom(s => s.AccessRestrictionMessage))
            .ForMember(d => d.EntitlementCount, opt => opt.MapFrom(s => s.Entitlements.Count(e => e.IsActive && !e.IsDeleted)))
            // Plan-level settings (for frontend display)
            .ForMember(d => d.GracePeriodDays, opt => opt.MapFrom(s => s.Plan.GracePeriodDays))
            .ForMember(d => d.ExportGraceDays, opt => opt.MapFrom(s => s.Plan.ExportGraceDays));

        CreateMap<Subscription, SubscriptionStatusDto>()
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan.Name))
            .ForMember(d => d.GracePeriodDays, opt => opt.MapFrom(s => s.Plan.GracePeriodDays))
            .ForMember(d => d.GraceEndDateUtc, opt => opt.MapFrom(s => s.ExpiryDateUtc.AddDays(s.Plan.GracePeriodDays)))
            .ForMember(d => d.NextSubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.NextSubscriptionId))
            .ForMember(d => d.NextSubscriptionPlanName, opt => opt.MapFrom(s => s.NextSubscription != null ? s.NextSubscription.Plan.Name : null))
            .ForMember(d => d.StatusMessage, opt => opt.Ignore()); // Set by service with localization

        CreateMap<CreateSubscriptionDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.Company, opt => opt.Ignore())
            .ForMember(d => d.Plan, opt => opt.Ignore())
            .ForMember(d => d.NextSubscription, opt => opt.Ignore())
            .ForMember(d => d.NextSubscriptionId, opt => opt.Ignore())
            .ForMember(d => d.NextSubscriptionActivationDateUtc, opt => opt.Ignore())
            .ForMember(d => d.ParentSubscription, opt => opt.Ignore())
            .ForMember(d => d.StartDateUtc, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.ExpiryDateUtc, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsActive, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsTrial, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsExpired, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.ParentSubscriptionId, opt => opt.Ignore())
            .ForMember(d => d.StatusReason, opt => opt.Ignore());

        // ========== Upgrade Subscription DTO - Decrypt NewPlanId ==========
        CreateMap<UpgradeNewPlanIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }
}
