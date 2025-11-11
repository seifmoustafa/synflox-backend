using AutoMapper;
using Domain.Entities.Licensing;
using Application.Services;
using Application.DTOs.Company;
using Application.DTOs.CompanyGroup;
using Application.DTOs.CompanyCustomField;
using Application.DTOs.Project;
using Application.DTOs.Module;
using Application.DTOs.ProjectModule;
using Application.DTOs.PlanProjectModule;
using Application.DTOs.SubscriptionHistory;

namespace Application.Mapping;

public class LicensingMappingProfile : Profile
{
    public LicensingMappingProfile()
    {
        CreateMap<Company, CompanyDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.SubscriptionPlanId,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.SubscriptionPlanId))
            .ForMember(d => d.LicenseKey, opt => opt.Ignore()); // Set manually based on user role

        CreateMap<CreateCompanyDto, Company>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true))
            .ForMember(d => d.LicenseKey, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCompanyDto, Company>()
            .ForMember(d => d.SubscriptionPlanId,
                opt => opt.ConvertUsing<DecryptStringToGuidConverter, string?>(s => s.SubscriptionPlanId))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Domain.Entities.Licensing.SubscriptionHistory, SubscriptionHistoryDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PerformedBy,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.PerformedBy))
            .ForMember(d => d.ActionTypeName,
                opt => opt.MapFrom(s => s.ActionType.ToString()));

        // Project mappings
        CreateMap<Domain.Entities.Licensing.Project, ProjectDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.Features)
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForMember(d => d.Modules, opt => opt.Ignore()); // Set manually in service

        CreateMap<CreateProjectDto, Domain.Entities.Licensing.Project>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? System.Text.Json.JsonSerializer.Serialize(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForMember(d => d.ProjectModules, opt => opt.Ignore())
            .ForMember(d => d.PlanProjectModules, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateProjectDto, Domain.Entities.Licensing.Project>()
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? System.Text.Json.JsonSerializer.Serialize(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Module mappings
        CreateMap<Domain.Entities.Licensing.Module, ModuleDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.Features)
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForMember(d => d.Projects, opt => opt.Ignore()); // Set manually in service

        CreateMap<CreateModuleDto, Domain.Entities.Licensing.Module>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? System.Text.Json.JsonSerializer.Serialize(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForMember(d => d.ProjectModules, opt => opt.Ignore())
            .ForMember(d => d.PlanProjectModules, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateModuleDto, Domain.Entities.Licensing.Module>()
            .ForMember(d => d.Features,
                opt => opt.MapFrom(s => s.Features != null && s.Features.Length > 0
                    ? System.Text.Json.JsonSerializer.Serialize(s.Features, new System.Text.Json.JsonSerializerOptions())
                    : null))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ProjectModule mappings
        CreateMap<Domain.Entities.Licensing.ProjectModule, ProjectModuleDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.ProjectId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.ProjectId))
            .ForMember(d => d.ModuleId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.ModuleId))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.Project != null ? s.Project.Name : "N/A"))
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : "N/A"));

        CreateMap<CreateProjectModuleDto, Domain.Entities.Licensing.ProjectModule>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ProjectId,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.ProjectId))
            .ForMember(d => d.ModuleId,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.ModuleId))
            .ForMember(d => d.Project, opt => opt.Ignore())
            .ForMember(d => d.Module, opt => opt.Ignore())
            .ForMember(d => d.PlanProjectModules, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        // PlanProjectModule mappings
        CreateMap<Domain.Entities.Licensing.PlanProjectModule, PlanProjectModuleDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.SubscriptionPlanId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.SubscriptionPlanId))
            .ForMember(d => d.ProjectModuleId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.ProjectModuleId))
            .ForMember(d => d.ProjectId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.ProjectModule.ProjectId))
            .ForMember(d => d.ModuleId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.ProjectModule.ModuleId))
            .ForMember(d => d.SubscriptionPlanName, opt => opt.MapFrom(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "N/A"))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.ProjectModule != null && s.ProjectModule.Project != null ? s.ProjectModule.Project.Name : "N/A"))
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.ProjectModule != null && s.ProjectModule.Module != null ? s.ProjectModule.Module.Name : "N/A"));

        // CompanyGroup mappings
        CreateMap<Domain.Entities.Licensing.CompanyGroup, CompanyGroupDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));

        CreateMap<CreateCompanyGroupDto, Domain.Entities.Licensing.CompanyGroup>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true))
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCompanyGroupDto, Domain.Entities.Licensing.CompanyGroup>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // CompanyCustomField mappings
        CreateMap<Domain.Entities.Licensing.CompanyCustomField, CompanyCustomFieldDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId));

        CreateMap<CreateCompanyCustomFieldDto, CompanyCustomField>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId, opt => opt.Ignore()) // Set manually in service/controller
            .ForMember(d => d.FieldType, opt => opt.MapFrom(s => s.FieldType.ToString()))
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCompanyCustomFieldDto, Domain.Entities.Licensing.CompanyCustomField>()
            .ForMember(d => d.FieldType, opt => opt.MapFrom(s => s.FieldType.HasValue ? s.FieldType.Value.ToString() : null))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

