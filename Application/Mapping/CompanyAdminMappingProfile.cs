using AutoMapper;
using Domain.Entities.Licensing;
using Application.DTOs.CompanyAdmin;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for CompanyAdmin entities.
/// Handles ID encryption in responses and decryption in requests.
/// </summary>
public class CompanyAdminMappingProfile : Profile
{
    public CompanyAdminMappingProfile()
    {
        // Entity → DTO (encrypt IDs)
        CreateMap<CompanyAdmin, CompanyAdminDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.CompanyName,
                opt => opt.MapFrom(s => s.Company != null ? s.Company.Name : string.Empty));

        CreateMap<CompanyAdmin, CompanyAdminDetailsDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.CompanyName,
                opt => opt.MapFrom(s => s.Company != null ? s.Company.Name : string.Empty));

        CreateMap<CompanyAdminSession, CompanyAdminSessionDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.EndReason,
                opt => opt.MapFrom(s => s.EndReason.ToString()));

        // Request DTOs → Guid (decrypt IDs)
        CreateMap<GetCompanyAdminByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<GetCompanyAdminByCompanyIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<DeleteCompanyAdminRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<UpdateCompanyAdminByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<ResetPasswordByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<UnlockAccountByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<TerminateSessionsByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // CreateCompanyAdminRequest - decrypt CompanyId
        CreateMap<CreateCompanyAdminRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // CompanyHasAdminRequest - decrypt CompanyId
        CreateMap<CompanyHasAdminRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }
}
