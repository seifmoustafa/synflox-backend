using AutoMapper;
using Application.DTOs.Settings;
using Application.Services;
using Domain.Entities.Settings;

namespace Application.Mapping;

public class PasswordPolicyMappingProfile : Profile
{
    public PasswordPolicyMappingProfile()
    {
        CreateMap<PasswordPolicy, PasswordPolicyDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));

        CreateMap<UpdatePasswordPolicyRequest, PasswordPolicy>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

