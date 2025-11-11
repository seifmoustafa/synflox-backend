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
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.LicenseKey, opt => opt.Ignore()); // Set manually based on user role

        CreateMap<CreateCompanyDto, Company>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.LicenseKey, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCompanyDto, Company>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
