using AutoMapper;
using Domain.Entities.Licensing;
using Application.DTOs.Company;

namespace Application.Mapping;

public class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        CreateMap<Company, CompanyDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));

        CreateMap<CreateCompanyDto, Company>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCompanyDto, Company>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Company request DTOs - decrypt CompanyId (using universal converter)
        CreateMap<GetCompanyByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        CreateMap<DeleteCompanyRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        CreateMap<UpdateCompanyByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        CreateMap<CompanyActionRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Custom Email request DTOs - decrypt CompanyId (using universal converter)
        CreateMap<CustomEmailRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }
}
