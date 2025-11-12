using AutoMapper;
using Domain.Entities.Subscriptions;
using Application.DTOs.Subscriptions;

namespace Application.Mapping;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        // ========== Project Mappings ==========
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.ProjectModules.Select(pm => pm.Module)));

        CreateMap<CreateProjectDto, Project>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ProjectModules, opt => opt.Ignore());

        CreateMap<UpdateProjectDto, Project>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        
        // Decrypt ModuleIds collection using request wrapper - returns IEnumerable, will be converted to List
        CreateMap<ModuleIdsRequest, IEnumerable<Guid>>()
            .ConvertUsing<RequestToGuidCollectionConverter<ModuleIdsRequest>>();
        
        // Decrypt ProjectIds collection using request wrapper - returns IEnumerable, will be converted to List
        CreateMap<ProjectIdsRequest, IEnumerable<Guid>>()
            .ConvertUsing<RequestToGuidCollectionConverter<ProjectIdsRequest>>();

        // ========== Module Mappings ==========
        CreateMap<Module, ModuleDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));

        CreateMap<CreateModuleDto, Module>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ProjectModules, opt => opt.Ignore())
            .ForMember(d => d.PlanModules, opt => opt.Ignore());

        CreateMap<UpdateModuleDto, Module>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ========== Plan Price Mappings ==========
        CreateMap<PlanPrice, PlanPriceDto>();
        CreateMap<PlanPriceDto, PlanPrice>();

        // ========== Subscription Plan Mappings ==========
        CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Prices, opt => opt.MapFrom(s => s.PlanPrices));

        CreateMap<SubscriptionPlan, PlanDetailsDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Prices, opt => opt.MapFrom(s => s.PlanPrices))
            .ForMember(d => d.Projects, opt => opt.MapFrom(s => 
                s.PlanProjects.Select(pp => pp.Project)))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => 
                s.PlanModules.Select(pm => pm.Module)));

        CreateMap<CreateSubscriptionPlanDto, SubscriptionPlan>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.PlanPrices, opt => opt.Ignore())
            .ForMember(d => d.PlanProjects, opt => opt.Ignore())
            .ForMember(d => d.PlanModules, opt => opt.Ignore())
            .ForMember(d => d.Subscriptions, opt => opt.Ignore());

        CreateMap<UpdateSubscriptionPlanDto, SubscriptionPlan>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ========== Subscription Mappings ==========
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan.Name))
            .ForMember(d => d.NextPlanId, opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.NextPlanId))
            .ForMember(d => d.NextPlanName, opt => opt.MapFrom(s => s.NextPlan != null ? s.NextPlan.Name : null))
            .ForMember(d => d.OfflineLicenseKey, opt => opt.Ignore()) // Set manually based on user role
            .ForMember(d => d.LicenseKeyGeneratedAt, opt => opt.MapFrom(s => s.LicenseKeyGeneratedAt))
            .ForMember(d => d.LicenseKeyVersion, opt => opt.MapFrom(s => s.LicenseKeyVersion));

        CreateMap<Subscription, SubscriptionStatusDto>()
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan.Name))
            .ForMember(d => d.GracePeriodDays, opt => opt.MapFrom(s => s.Plan.GracePeriodDays))
            .ForMember(d => d.GraceEndDateUtc, opt => opt.MapFrom(s => s.ExpiryDateUtc.AddDays(s.Plan.GracePeriodDays)))
            .ForMember(d => d.NextPlanId, opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.NextPlanId))
            .ForMember(d => d.NextPlanName, opt => opt.MapFrom(s => s.NextPlan != null ? s.NextPlan.Name : null))
            .ForMember(d => d.StatusMessage, opt => opt.Ignore()); // Set by service with localization

        CreateMap<CreateSubscriptionDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.NextPlanId, opt => opt.ConvertUsing<DecryptNullableGuidConverter, Guid?>(s => s.NextPlanId))
            .ForMember(d => d.Company, opt => opt.Ignore())
            .ForMember(d => d.Plan, opt => opt.Ignore())
            .ForMember(d => d.NextPlan, opt => opt.Ignore())
            .ForMember(d => d.ParentSubscription, opt => opt.Ignore())
            .ForMember(d => d.StartDateUtc, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.ExpiryDateUtc, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsActive, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsTrial, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.IsExpired, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.ParentSubscriptionId, opt => opt.Ignore())
            .ForMember(d => d.StatusReason, opt => opt.Ignore())
            .ForMember(d => d.UpgradePolicyOverride, opt => opt.Ignore());
    }
}
