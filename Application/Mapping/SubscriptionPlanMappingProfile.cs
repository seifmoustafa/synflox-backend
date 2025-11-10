using AutoMapper;
using Domain.Entities.Licensing;
using Application.DTOs.Licensing;
using Application.Services;
using System.Text.Json;

namespace Application.Mapping;

public class SubscriptionPlanMappingProfile : Profile
{
    public SubscriptionPlanMappingProfile()
    {
        CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.Features)
                    ? JsonSerializer.Deserialize<string[]>(s.Features, new JsonSerializerOptions())
                    : null))
            .ForMember(d => d.ParentPlanId,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.ParentPlanId))
            .ForMember(d => d.ParentPlanName,
                opt => opt.MapFrom(s => s.ParentPlan != null ? s.ParentPlan.Name : null));

        CreateMap<SubscriptionPlan, PlanFeaturesDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.OwnFeatures, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.AllFeatures, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.ProjectModules, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.InheritedProjectModules, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.ParentPlan, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.ChildPlans, opt => opt.Ignore()); // Set manually in service

        CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlan>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? JsonSerializer.Serialize(s.Features, new JsonSerializerOptions())
                    : null))
            .ForMember(d => d.ParentPlanId,
                opt => opt.ConvertUsing<DecryptStringToGuidConverter, string?>(s => s.ParentPlanId))
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true));

        CreateMap<UpdateSubscriptionPlanRequest, SubscriptionPlan>()
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? JsonSerializer.Serialize(s.Features, new JsonSerializerOptions())
                    : null))
            .ForMember(d => d.ParentPlanId,
                opt => opt.ConvertUsing<DecryptStringToGuidConverter, string?>(s => s.ParentPlanId))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

