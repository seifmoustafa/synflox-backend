using AutoMapper;
using Domain.Entities.Authentication;
using Application.DTOs.Admin;
using Application.DTOs.AdminType;
using Application.Services;

namespace Application.Mapping;

public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<Admin, AdminDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.AdminTypeId,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.AdminTypeId))
            .ForMember(d => d.AdminTypeName,
                opt => opt.MapFrom(s => s.AdminType.AdminTypeName));

        CreateMap<CreateAdminDto, Admin>()
            .ForMember(d => d.Password, opt => opt.Ignore())
            .ForMember(d => d.AdminTypeId, 
                opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid>(s => s.AdminTypeId));

        CreateMap<UpdateAdminRequest, Admin>()
            .ForMember(d => d.AdminTypeId, 
                opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid>(s => s.AdminTypeId))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<AdminType, AdminTypeDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));

        CreateMap<CreateAdminTypeDto, AdminType>();
        
        CreateMap<UpdateAdminTypeDto, AdminType>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // AdminType request DTOs - decrypt AdminTypeId (using universal converter)
        CreateMap<GetAdminTypeByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<UpdateAdminTypeByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // Admin request DTOs - decrypt AdminId (using universal converter)
        CreateMap<GetAdminByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<UpdateAdminByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<ChangePasswordByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // Admin bulk operations - decrypt AdminIds collection (using universal converter)
        CreateMap<AdminIdsRequest, IEnumerable<Guid>>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }
}

