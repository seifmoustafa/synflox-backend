using AutoMapper;
using Domain.Entities.Licensing;
using Application.DTOs.Company;
using Application.DTOs.Licensing;

namespace Application.Mapping;

public class LicensingMappingProfile : Profile
{
    public LicensingMappingProfile()
    {
        CreateMap<Company, CompanyDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
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
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Licensing request DTOs - decrypt CompanyId
        CreateMap<ActivateCompanyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<ExtendCompanyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<SuspendCompanyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<ResumeCompanyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<GenerateLicenseKeyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId)); 

        CreateMap<GetCompanyStatusRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        // Company request DTOs - decrypt CompanyId
        CreateMap<GetCompanyByIdRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<UpdateCompanyByIdRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        CreateMap<DeleteCompanyRequest, Guid>()
            .ForMember(d => d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.CompanyId));

        // Response DTOs - encrypt CompanyId for client
        CreateMap<Company, LicenseKeyValidationResponse>()
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.IsValid, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Message, opt => opt.Ignore())
            .ForMember(d => d.ClockTampered, opt => opt.Ignore());
    }
}

