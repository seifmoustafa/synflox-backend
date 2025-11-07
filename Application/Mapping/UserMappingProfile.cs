using AutoMapper;
using Domain.Entities.Authentication;
using Application.DTOs.Authentication;
using Application.Services;
using Application.DTOs.User;
using Application.DTOs.Admin;
using Application.DTOs.AdminType;

namespace Application.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.Gender,
                opt => opt.MapFrom(s => s.Gender.HasValue ? s.Gender.Value.ToString() : null))
            .ForMember(d => d.Providers,
                opt => opt.MapFrom(s => s.Providers.ToString()))
            .ForMember(d => d.IsEmailVerified,
                opt => opt.MapFrom(s => s.IsEmailVerified))
            .ForMember(d => d.IsPhoneVerified,
                opt => opt.MapFrom(s => s.IsPhoneVerified))
            .ForMember(d => d.IsVerified,
                opt => opt.MapFrom(s => s.IsVerified))
            .ForMember(d => d.IsActive,
                opt => opt.MapFrom(s => s.IsActive))
            .ForMember(d => d.LastLogin,
                opt => opt.MapFrom(s => s.LastLogin));

        CreateMap<Admin, AdminDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.AdminTypeName,
                opt => opt.MapFrom(s => s.AdminType.AdminTypeName));

        CreateMap<ICurrentUserService, UserDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.UserId));
          

        CreateMap<RegistrationRequest, User>()
            .ForMember(d => d.Password, opt => opt.Ignore())
            .ForMember(d => d.ImagePath, opt => opt.Ignore());

        CreateMap<RegistrationRequest, Admin>()
            .ForMember(d => d.Password, opt => opt.Ignore());

        CreateMap<UpdateProfileRequest, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateUserDto, User>();
        CreateMap<CreateAdminDto, Admin>()
            .ForMember(d => d.Password, opt => opt.Ignore());
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
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
