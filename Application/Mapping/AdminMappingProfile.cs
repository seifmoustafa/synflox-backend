using AutoMapper;
using Domain.Entities.Authentication;
using Application.DTOs.Admin;
using Application.DTOs.AdminType;

namespace Application.Mapping;

public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<Admin, AdminDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.AdminTypeId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.AdminTypeId))
            .ForMember(d => d.AdminTypeName,
                opt => opt.MapFrom(s => s.AdminType.AdminTypeName));

        CreateMap<CreateAdminDto, Admin>()
            .ForMember(d => d.Password, opt => opt.Ignore())
            .ForMember(d => d.AdminTypeId, 
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.AdminTypeId));

        CreateMap<UpdateAdminRequest, Admin>()
            .ForMember(d => d.AdminTypeId, 
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.AdminTypeId))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<AdminType, AdminTypeDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));

        CreateMap<CreateAdminTypeDto, AdminType>();
        
        CreateMap<UpdateAdminTypeDto, AdminType>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // AdminType request DTOs - decrypt AdminTypeId
        CreateMap<GetAdminTypeByIdRequest, Guid>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.AdminTypeId));

        CreateMap<UpdateAdminTypeByIdRequest, Guid>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.AdminTypeId));

        // Admin request DTOs - decrypt AdminId
        CreateMap<GetAdminByIdRequest, Guid>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.AdminId));

        CreateMap<UpdateAdminByIdRequest, Guid>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.AdminId));

        CreateMap<ChangePasswordByIdRequest, Guid>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(src => src.AdminId));

        // Admin bulk operations - decrypt AdminIds collection
        CreateMap<AdminIdsRequest, IEnumerable<Guid>>()
            .ForMember(d=> d,
                opt => opt.ConvertUsing<DecryptGuidCollectionConverter, IEnumerable<Guid>>(src => src.AdminIds));
    }
}

