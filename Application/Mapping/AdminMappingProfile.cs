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
            .ForMember(d => d.AdminTypeName,
                opt => opt.MapFrom(s => s.AdminType.AdminTypeName));

        CreateMap<CreateAdminDto, Admin>()
            .ForMember(d => d.Password, opt => opt.Ignore());

        CreateMap<UpdateAdminRequest, Admin>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<AdminType, AdminTypeDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));

        CreateMap<CreateAdminTypeDto, AdminType>();
        
        CreateMap<UpdateAdminTypeDto, AdminType>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

